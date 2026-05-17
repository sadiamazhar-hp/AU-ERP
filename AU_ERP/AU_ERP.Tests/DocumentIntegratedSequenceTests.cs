using AU_ERP.Models;
using AU_ERP.Services;
using Xunit;

namespace AU_ERP.Tests;

public class DocumentIntegratedSequenceTests
{
    [Fact]
    public void Fresh_range_first_issue_matches_range_start_when_db_has_no_documents()
    {
        var ranges = new List<DocumentRange>
        {
            new()
            {
                RangeID = 1,
                FromNumber = 2000,
                ToNumber = 2200,
                LastIssuedNumber = 1999
            }
        };
        Assert.True(DocumentIntegratedSequence.TryPickNextAcrossOrderedRanges(ranges, 0, out _, out var next));
        Assert.Equal(2000, next);
    }

    [Fact]
    public void Existing_db_documents_raise_next_above_max_suffix_gaps_accepted()
    {
        var ranges = new List<DocumentRange>
        {
            new()
            {
                RangeID = 1,
                FromNumber = 2000,
                ToNumber = 2200,
                LastIssuedNumber = 1999 // counter never moved; SO-2050 already in DB manually
            }
        };
        Assert.True(DocumentIntegratedSequence.TryPickNextAcrossOrderedRanges(ranges, 2050, out _, out var next));
        Assert.Equal(2051, next);
    }

    [Fact]
    public void Stored_LastIssued_wins_when_higher_than_db_max()
    {
        var ranges = new List<DocumentRange>
        {
            new()
            {
                RangeID = 1,
                FromNumber = 2000,
                ToNumber = 2200,
                LastIssuedNumber = 2090 // allocator already persisted
            }
        };
        Assert.True(DocumentIntegratedSequence.TryPickNextAcrossOrderedRanges(ranges, 2080, out _, out var next));
        Assert.Equal(2091, next);
    }

    [Fact]
    public void Second_segment_selected_when_prior_segment_cannot_hold_candidate_but_next_can()
    {
        var ranges = new List<DocumentRange>
        {
            new()
            {
                RangeID = 1,
                FromNumber = 2000,
                ToNumber = 2200,
                LastIssuedNumber = null // floor 1999; maxExisting 3199 → candidate 3200 does not fit
            },
            new()
            {
                RangeID = 2,
                FromNumber = 3000,
                ToNumber = 3200,
                LastIssuedNumber = 3199
            }
        };
        Assert.True(DocumentIntegratedSequence.TryPickNextAcrossOrderedRanges(ranges, 3199, out var chosen, out var next));
        Assert.NotNull(chosen);
        Assert.Equal(2, chosen.RangeID);
        Assert.Equal(3200, next);
    }

    [Fact]
    public void Exhausted_returns_false_when_no_segment_can_fit_candidate()
    {
        var ranges = new List<DocumentRange>
        {
            new()
            {
                RangeID = 1,
                FromNumber = 2000,
                ToNumber = 2001,
                LastIssuedNumber = 1999 // next would jump to 9500 via maxDb → no fit
            }
        };
        Assert.False(DocumentIntegratedSequence.TryPickNextAcrossOrderedRanges(ranges, 9499, out _, out _));
    }

    [Fact]
    public void Parses_doc_code_numeric_suffix_without_reusing_manual_edits_under_prefix()
    {
        Assert.True(DocumentIntegratedSequence.TryParseDocCodeSuffix("SO", "SO-2001", out var n));
        Assert.Equal(2001, n);
        Assert.Equal(2099,
            DocumentIntegratedSequence.MaxSuffixFromDocumentNumbers(["SO-2001", "so-2099"], "SO"));
        Assert.False(DocumentIntegratedSequence.TryParseDocCodeSuffix("SO", "INV-2099", out _));
    }

    /// <summary>Placeholder for deterministic allocator math; concurrency is enforced via Serializable EF transactions.</summary>
    [Fact]
    public void Repeated_allocation_math_is_serializable_by_formula()
    {
        var ranges = new List<DocumentRange>
        {
            new() { RangeID = 1, FromNumber = 1, ToNumber = long.MaxValue, LastIssuedNumber = 41 }
        };
        Assert.True(DocumentIntegratedSequence.TryPickNextAcrossOrderedRanges(ranges, 100, out _, out var n1));
        Assert.Equal(101, n1);
        ranges[0].LastIssuedNumber = n1;
        Assert.True(DocumentIntegratedSequence.TryPickNextAcrossOrderedRanges(ranges, 100, out _, out var n2));
        Assert.Equal(102, n2);
    }

    [Fact]
    public void Parses_leading_zeros_in_suffix()
    {
        Assert.True(DocumentIntegratedSequence.TryParseDocCodeSuffix("SO", "SO-02000", out var s));
        Assert.Equal(2000, s);
    }

    [Fact]
    public void Rejects_bad_suffix_patterns()
    {
        Assert.False(DocumentIntegratedSequence.TryParseDocCodeSuffix("SO", "--bad", out _));
        Assert.False(DocumentIntegratedSequence.TryParseDocCodeSuffix("SO", "SO-", out _));
        Assert.False(DocumentIntegratedSequence.TryParseDocCodeSuffix("SO", "SO-ABC", out _));
        Assert.False(DocumentIntegratedSequence.TryParseDocCodeSuffix("SO", "INV-2099", out _));
    }
}
