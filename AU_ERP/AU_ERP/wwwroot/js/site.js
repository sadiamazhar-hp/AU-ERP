// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(function () {
    function moveMaterialSuccessModalToBody() {
        var el = document.getElementById("materialSuccessModal");
        if (el && el.parentElement !== document.body)
            document.body.appendChild(el);
    }

    function moveMaterialDeleteConfirmModalToBody() {
        var el = document.getElementById("materialDeleteConfirmModal");
        if (el && el.parentElement !== document.body)
            document.body.appendChild(el);
    }

    /**
     * Full navigation to Materials page with ?tab=list so the server renders the List tab on first paint (no Create-tab flash).
     */
    window.materialsReloadToListTab = function () {
        try {
            var u = new URL(window.location.href);
            u.searchParams.set("tab", "list");
            var q = u.searchParams.toString();
            window.location.assign(u.pathname + (q ? "?" + q : ""));
        } catch (e) {
            window.location.reload();
        }
    };

    /**
     * Shows the teal Material success modal; runs onHiddenOnce after it is fully hidden (OK or Esc).
     */
    window.materialShowSuccessModalThen = function (message, onHiddenOnce) {
        var bodyEl = document.getElementById("materialSuccessModalBody");
        if (bodyEl) bodyEl.textContent = message || "";
        var el = document.getElementById("materialSuccessModal");
        if (!el || typeof bootstrap === "undefined" || !bootstrap.Modal) {
            if (typeof onHiddenOnce === "function") onHiddenOnce();
            return;
        }
        moveMaterialSuccessModalToBody();
        var modal = bootstrap.Modal.getOrCreateInstance(el);
        function handleHidden() {
            el.removeEventListener("hidden.bs.modal", handleHidden);
            if (typeof onHiddenOnce === "function") onHiddenOnce();
        }
        el.addEventListener("hidden.bs.modal", handleHidden);
        modal.show();
    };

    window.materialShowSuccessModal = function (message) {
        window.materialShowSuccessModalThen(message, function () { });
    };

    /**
     * Maroon one-button ERROR modal.
     */
    window.materialShowErrorModal = function (message) {
        var text = message || "Operation failed.";
        var bodyEl = document.getElementById("bpRangeErrorModalBody");
        if (bodyEl) bodyEl.textContent = text;
        var el = document.getElementById("bpRangeErrorModal");
        if (!el || typeof bootstrap === "undefined" || !bootstrap.Modal) {
            window.alert(text);
            return;
        }
        if (el.parentElement !== document.body)
            document.body.appendChild(el);
        var modal = bootstrap.Modal.getOrCreateInstance(el);
        modal.show();
    };

    /**
     * Maroon WARNING modal; onConfirm runs only if user clicks OK (not Cancel / Esc / backdrop).
     */
    window.materialShowDeleteConfirmModalChoice = function (message, onDecision) {
        var text = message || "Do you want to Delete this Material?";
        var bodyEl = document.getElementById("materialDeleteConfirmModalBody");
        if (bodyEl) bodyEl.textContent = text;

        var el = document.getElementById("materialDeleteConfirmModal");
        var okBtn = document.getElementById("materialDeleteConfirmModalOk");
        if (!el || !okBtn || typeof bootstrap === "undefined" || !bootstrap.Modal) {
            if (typeof onDecision === "function") onDecision(!!window.confirm(text));
            return;
        }

        moveMaterialDeleteConfirmModalToBody();
        var modal = bootstrap.Modal.getOrCreateInstance(el);
        var confirmed = false;

        function onOkClick(ev) {
            ev.preventDefault();
            confirmed = true;
            modal.hide();
        }

        function onHidden() {
            el.removeEventListener("hidden.bs.modal", onHidden);
            okBtn.removeEventListener("click", onOkClick);
            if (typeof onDecision === "function") onDecision(confirmed);
        }

        okBtn.addEventListener("click", onOkClick);
        el.addEventListener("hidden.bs.modal", onHidden, { once: true });
        modal.show();
    };

    window.materialShowDeleteConfirmModalThen = function (message, onConfirm) {
        window.materialShowDeleteConfirmModalChoice(message, function (confirmed) {
            if (confirmed && typeof onConfirm === "function") onConfirm();
        });
    };

    /**
     * Clears inline validation state under root (invalid field borders + summary text).
     */
    window.auClearInlineValidation = function (root) {
        if (!root) return;
        if (typeof root === "string") root = document.querySelector(root);
        if (!root) return;
        root.querySelectorAll(".au-field-invalid").forEach(function (el) {
            el.classList.remove("au-field-invalid");
        });
        root.querySelectorAll(".au-validation-summary").forEach(function (s) {
            s.style.display = "none";
            s.textContent = "";
        });
    };

    /**
     * Shows fixed or custom message and red borders on invalid fields (no modal).
     * @param {HTMLElement|string} root
     * @param {HTMLElement[]} invalidEls
     * @param {string} [message] defaults to "Following Fields are Required"
     */
    window.auShowInlineValidation = function (root, invalidEls, message) {
        if (typeof root === "string") root = document.querySelector(root);
        if (!root) return false;
        window.auClearInlineValidation(root);
        var text = message || "Following Fields are Required";
        var s = root.querySelector(".au-validation-summary");
        if (!s) {
            s = document.createElement("div");
            s.className = "au-validation-summary";
            s.setAttribute("role", "alert");
            var tb = root.querySelector(".sap-toolbar");
            if (tb && tb.parentNode) tb.parentNode.insertBefore(s, tb.nextSibling);
            else root.insertBefore(s, root.firstChild);
        }
        s.textContent = text;
        s.style.display = "block";
        (invalidEls || []).forEach(function (el) {
            if (el && el.classList) el.classList.add("au-field-invalid");
        });
        if (invalidEls && invalidEls.length && invalidEls[0]) {
            try {
                invalidEls[0].focus();
            } catch (e) { /* ignore */ }
        }
        return false;
    };

    /**
     * Finds the UI block scope for inline validation: first ancestor whose first-level children include .au-validation-summary
     * (covers wc-create-form, bom-create-form, sap forms, BP create, modal-body patterns).
     */
    function auFindInlineValidationScope(fromEl) {
        for (var p = fromEl.parentElement; p; p = p.parentElement) {
            for (var c = p.firstElementChild; c; c = c.nextElementSibling) {
                if (c.classList && c.classList.contains("au-validation-summary")) return p;
            }
        }
        return null;
    }

    function auOnFieldMaybeClearInlineValidation(ev) {
        var target = ev.target;
        if (!target || target.nodeType !== 1) return;
        var tag = target.tagName;
        if (tag !== "INPUT" && tag !== "TEXTAREA" && tag !== "SELECT") return;
        if (tag === "INPUT") {
            var tp = target.type || "";
            if (
                tp === "hidden" ||
                tp === "button" ||
                tp === "submit" ||
                tp === "reset" ||
                tp === "file"
            )
                return;
        }
        var root = auFindInlineValidationScope(target);
        if (!root) return;
        if (target.classList && target.classList.contains("au-field-invalid"))
            target.classList.remove("au-field-invalid");
        if (!root.querySelector(".au-field-invalid")) window.auClearInlineValidation(root);
    }

    document.addEventListener("input", auOnFieldMaybeClearInlineValidation, true);
    document.addEventListener("change", auOnFieldMaybeClearInlineValidation, true);
})();

/**
 * Enforces non-negative values on number inputs app-wide.
 * Opt out per field with attribute data-allow-negative="true" (e.g. temperature deltas).
 */
/**
 * Operational document quantities: whole numbers only, optional minimum (default 1).
 * Use data-au-qty-min="0" for fields that allow zero (e.g. scrap bucket splits).
 * Do not add this class to money fields, tax rates, or time-in-hours inputs.
 */
(function () {
    function auDocQtyMin(el) {
        var a = el.getAttribute("data-au-qty-min");
        if (a === "0") return 0;
        return 1;
    }
    function isAuDocQty(el) {
        return el && el.tagName === "INPUT" && el.type === "number" && el.classList && el.classList.contains("au-doc-qty");
    }
    function coerceAuDocQty(el) {
        if (!isAuDocQty(el)) return;
        var minV = auDocQtyMin(el);
        var raw = String(el.value || "").trim();
        if (raw === "" || raw === "-") return;
        var digits = raw.replace(/\D/g, "");
        if (digits === "") {
            el.value = "";
            return;
        }
        var n = parseInt(digits, 10);
        if (isNaN(n)) {
            el.value = "";
            return;
        }
        if (n < minV) n = minV;
        el.value = String(n);
    }
    document.addEventListener(
        "input",
        function (e) {
            coerceAuDocQty(e.target);
        },
        true
    );
})();

(function () {
    function skip(el) {
        if (!el || el.tagName !== "INPUT" || el.type !== "number") return true;
        if (el.hasAttribute("data-allow-negative")) return true;
        if (el.classList && el.classList.contains("au-doc-qty")) return true;
        return false;
    }
    function clampNonNegative(el) {
        if (skip(el)) return;
        var raw = el.value;
        if (raw === "" || raw == null) return;
        var n = parseFloat(raw);
        if (isNaN(n)) return;
        if (n < 0) {
            el.value = "0";
            try {
                el.dispatchEvent(new Event("input", { bubbles: true }));
                el.dispatchEvent(new Event("change", { bubbles: true }));
            } catch (e) { /* ignore */ }
        }
    }
    document.addEventListener("blur", function (e) {
        clampNonNegative(e.target);
    }, true);
    document.addEventListener("change", function (e) {
        clampNonNegative(e.target);
    }, true);
})();

(function () {
    /** Sales-grade money fields / line unit price: disallow negative values while typing */
    function auClampSalesMoneyInput(el) {
        if (!el || el.tagName !== "INPUT" || el.type !== "number") return;
        var c = el.classList;
        if (
            !c ||
            (!c.contains("mat-sale-pkr") &&
                !c.contains("v2-mat-sale-pkr") &&
                !c.contains("sq-unit") &&
                !c.contains("so-unit"))
        )
            return;
        var raw = el.value;
        if (raw === "" || raw === "-" || raw === ".") return;
        var n = parseFloat(raw);
        if (!isNaN(n) && n < 0) {
            el.value = "0";
            try {
                el.dispatchEvent(new Event("input", { bubbles: true }));
                el.dispatchEvent(new Event("change", { bubbles: true }));
            } catch (err) {
                /* ignore */
            }
        }
    }

    document.addEventListener(
        "input",
        function (e) {
            auClampSalesMoneyInput(e.target);
        },
        true
    );
})();

(function () {
    /** Strip leading "-" from barcode-style text (EAN cannot be entered as negative) */
    function auStripLeadingMinusText(el) {
        if (
            !el ||
            el.tagName !== "INPUT" ||
            (el.type !== "text" && el.type !== "tel" && el.type !== "search")
        )
            return;
        if (!el.classList || !el.classList.contains("au-ean-upc")) return;
        var v = el.value;
        if (!v.startsWith("-")) return;
        el.value = v.replace(/^-+/, "");
    }

    document.addEventListener(
        "input",
        function (e) {
            auStripLeadingMinusText(e.target);
        },
        true
    );
})();

(function () {
    /** Licence / plates: reject a bare negative number; drop a leading '-' if user mis-clicks. */
    function auFleetTextNoBareNegative(el, cls) {
        if (!el || el.tagName !== "INPUT" || !el.classList || !el.classList.contains(cls)) return;
        var raw = el.value;
        if (/^-\d+$/.test(raw.trim())) {
            el.value = "";
            return;
        }
        if (raw.startsWith("-")) el.value = raw.replace(/^-+/, "");
    }

    document.addEventListener(
        "input",
        function (e) {
            auFleetTextNoBareNegative(e.target, "au-driver-licence-nneg");
            auFleetTextNoBareNegative(e.target, "au-vehicle-plate-nneg");
        },
        true
    );
})();

/**
 * After navigation, the window scrolls to top. Scroll the fixed sidebar so the active link remains in view.
 */
(function () {
    function auScrollSidebarToActive() {
        var side = document.querySelector(".au-sidebar");
        if (!side) return;
        var active = side.querySelector(".au-sidebar-link.active");
        if (!active) return;
        var sr = side.getBoundingClientRect();
        var ar = active.getBoundingClientRect();
        var delta = (ar.top + ar.height / 2) - (sr.top + sr.height / 2);
        side.scrollTop = Math.max(0, Math.min(side.scrollTop + delta, side.scrollHeight - side.clientHeight));
    }
    if (document.readyState === "loading")
        document.addEventListener("DOMContentLoaded", auScrollSidebarToActive);
    else
        auScrollSidebarToActive();
})();

/**
 * In long document modals, Enter in inputs often triggers implicit form submit (first submit button) and navigates away.
 * Opt in per form with attribute data-au-prevent-enter-submit. Textareas remain unchanged so users can add line breaks intentionally.
 */
(function () {
    function shouldAllowEnterDefault(target) {
        if (!target) return false;
        var tag = (target.tagName || "").toUpperCase();
        if (tag === "TEXTAREA") return true;
        if (tag === "BUTTON" || tag === "A") return true;
        if (tag === "INPUT") {
            var ty = String(target.type || "").toLowerCase();
            if (ty === "submit" || ty === "button") return true;
            if (ty === "checkbox" || ty === "radio" || ty === "file") return true;
        }
        if (target.isContentEditable) return true;
        return false;
    }
    document.addEventListener(
        "keydown",
        function (e) {
            if (e.key !== "Enter" || e.defaultPrevented || e.repeat) return;
            if (typeof e.isComposing === "boolean" && e.isComposing) return;
            var t = e.target;
            if (!t || typeof t.closest !== "function") return;
            var form = t.closest("form[data-au-prevent-enter-submit]");
            if (!form) return;
            if (shouldAllowEnterDefault(t)) return;
            e.preventDefault();
            e.stopPropagation();
        },
        true
    );
})();
