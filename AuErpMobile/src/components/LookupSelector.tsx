import React, {useCallback, useEffect, useState} from 'react';
import {
  ActivityIndicator,
  FlatList,
  Modal,
  Pressable,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View,
} from 'react-native';
import Icon from 'react-native-vector-icons/MaterialIcons';
import {Colors} from '../theme/colors';

interface Props {
  label: string;
  placeholder: string;
  valueId: string;
  valueLabel: string;
  onSelect: (id: string, label: string) => void;
  fetchItems: (search: string) => Promise<{id: string; label: string}[]>;
  icon?: string;
}

export default function LookupSelector({
  label,
  placeholder,
  valueId,
  valueLabel,
  onSelect,
  fetchItems,
  icon = 'search',
}: Props) {
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState('');
  const [items, setItems] = useState<{id: string; label: string}[]>([]);
  const [loading, setLoading] = useState(false);

  const load = useCallback(
    async (q: string) => {
      setLoading(true);
      try {
        const list = await fetchItems(q);
        setItems(list);
      } catch {
        setItems([]);
      } finally {
        setLoading(false);
      }
    },
    [fetchItems],
  );

  useEffect(() => {
    if (!open) return;
    const t = setTimeout(() => load(search), 250);
    return () => clearTimeout(t);
  }, [open, search, load]);

  return (
    <>
      <Text style={styles.sectionLabel}>{label}</Text>
      <View style={styles.field}>
        <TouchableOpacity style={styles.fieldMain} onPress={() => setOpen(true)}>
          <Icon name={icon} size={16} color={Colors.subtle} style={{marginRight: 8}} />
          <Text style={[styles.fieldText, !valueLabel && styles.placeholder]} numberOfLines={1}>
            {valueLabel || placeholder}
          </Text>
        </TouchableOpacity>
        {valueId ? (
          <TouchableOpacity onPress={() => onSelect('', '')} hitSlop={{top: 8, bottom: 8, left: 8, right: 8}}>
            <Icon name="close" size={16} color={Colors.subtle} />
          </TouchableOpacity>
        ) : (
          <Icon name="chevron-right" size={20} color={Colors.subtle} />
        )}
      </View>

      <Modal visible={open} animationType="slide" transparent onRequestClose={() => setOpen(false)}>
        <Pressable style={styles.backdrop} onPress={() => setOpen(false)} />
        <View style={styles.sheet}>
          <View style={styles.sheetHeader}>
            <Text style={styles.heading}>Select {label}</Text>
            <TouchableOpacity onPress={() => setOpen(false)}>
              <Icon name="close" size={20} color={Colors.subtle} />
            </TouchableOpacity>
          </View>
          <View style={styles.searchWrap}>
            <Icon name="search" size={18} color={Colors.subtle} />
            <TextInput
              style={styles.searchInput}
              value={search}
              onChangeText={setSearch}
              placeholder={`Search ${label.toLowerCase()}…`}
              placeholderTextColor={Colors.placeholder}
              autoFocus
            />
          </View>
          {loading ? (
            <ActivityIndicator color={Colors.blue} style={{marginVertical: 24}} />
          ) : (
            <FlatList
              data={items}
              keyExtractor={item => item.id}
              keyboardShouldPersistTaps="handled"
              ListEmptyComponent={<Text style={styles.empty}>No results</Text>}
              renderItem={({item}) => (
                <TouchableOpacity
                  style={styles.item}
                  onPress={() => {
                    onSelect(item.id, item.label);
                    setOpen(false);
                    setSearch('');
                  }}>
                  <Text style={styles.itemTitle}>{item.label}</Text>
                  <Text style={styles.itemSub}>{item.id}</Text>
                </TouchableOpacity>
              )}
            />
          )}
        </View>
      </Modal>
    </>
  );
}

const styles = StyleSheet.create({
  sectionLabel: {
    fontSize: 11,
    fontWeight: '700',
    color: Colors.subtle,
    textTransform: 'uppercase',
    letterSpacing: 0.8,
    marginBottom: 8,
    marginTop: 16,
  },
  field: {
    flexDirection: 'row',
    alignItems: 'center',
    borderWidth: 1.5,
    borderColor: Colors.border,
    borderRadius: 10,
    paddingHorizontal: 12,
    paddingVertical: 11,
    backgroundColor: Colors.bg,
  },
  fieldMain: {flex: 1, flexDirection: 'row', alignItems: 'center'},
  fieldText: {flex: 1, fontSize: 14, color: Colors.text},
  placeholder: {color: Colors.placeholder},
  backdrop: {flex: 1, backgroundColor: 'rgba(10,22,40,0.5)'},
  sheet: {
    backgroundColor: Colors.card,
    borderTopLeftRadius: 24,
    borderTopRightRadius: 24,
    padding: 20,
    maxHeight: '70%',
  },
  sheetHeader: {flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12},
  heading: {fontSize: 17, fontWeight: '800', color: Colors.text},
  searchWrap: {
    flexDirection: 'row',
    alignItems: 'center',
    borderWidth: 1.5,
    borderColor: Colors.border,
    borderRadius: 10,
    paddingHorizontal: 10,
    marginBottom: 12,
    gap: 8,
  },
  searchInput: {flex: 1, paddingVertical: 10, fontSize: 14, color: Colors.text},
  item: {paddingVertical: 12, borderBottomWidth: 1, borderBottomColor: Colors.divider},
  itemTitle: {fontSize: 14, fontWeight: '600', color: Colors.text},
  itemSub: {fontSize: 11, color: Colors.subtle, marginTop: 2},
  empty: {textAlign: 'center', color: Colors.subtle, padding: 24},
});
