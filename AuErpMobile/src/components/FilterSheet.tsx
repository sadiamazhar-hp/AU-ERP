import React, {useState} from 'react';
import {
  Modal,
  Platform,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View,
} from 'react-native';
import DateTimePicker from '@react-native-community/datetimepicker';
import Icon from 'react-native-vector-icons/MaterialIcons';
import {Colors} from '../theme/colors';

export interface FilterValues {
  dateFrom: Date | null;
  dateTo: Date | null;
  plantId: string;
  customerId: string;
}

interface Props {
  visible: boolean;
  values: FilterValues;
  onApply: (v: FilterValues) => void;
  onClose: () => void;
  showPlant?: boolean;
  showCustomer?: boolean;
}

type DateField = 'dateFrom' | 'dateTo' | null;

export default function FilterSheet({visible, values, onApply, onClose, showPlant, showCustomer}: Props) {
  const [local, setLocal] = useState<FilterValues>(values);
  const [pickerField, setPickerField] = useState<DateField>(null);

  const fmt = (d: Date | null) => (d ? d.toLocaleDateString('en-GB', {day: '2-digit', month: 'short', year: 'numeric'}) : 'Any date');

  function handleDateChange(_: unknown, date?: Date) {
    if (Platform.OS === 'android') setPickerField(null);
    if (date && pickerField) setLocal(v => ({...v, [pickerField]: date}));
  }

  function reset() {
    setLocal({dateFrom: null, dateTo: null, plantId: '', customerId: ''});
  }

  return (
    <Modal visible={visible} animationType="slide" transparent onRequestClose={onClose}>
      <Pressable style={styles.backdrop} onPress={onClose} />
      <View style={styles.sheet}>
        {/* Handle */}
        <View style={styles.handle} />

        {/* Header */}
        <View style={styles.sheetHeader}>
          <Text style={styles.heading}>Filter Report</Text>
          <TouchableOpacity onPress={onClose} style={styles.closeBtn}>
            <Icon name="close" size={20} color={Colors.subtle} />
          </TouchableOpacity>
        </View>

        <ScrollView showsVerticalScrollIndicator={false}>
          {/* Date range */}
          <Text style={styles.sectionLabel}>Date Range</Text>
          <View style={styles.dateRow}>
            <View style={styles.dateFlex}>
              <Text style={styles.label}>From</Text>
              <TouchableOpacity style={styles.dateBtn} onPress={() => setPickerField('dateFrom')}>
                <Icon name="calendar-today" size={14} color={Colors.blue} style={{marginRight: 6}} />
                <Text style={[styles.dateTxt, !local.dateFrom && styles.datePlaceholder]}>{fmt(local.dateFrom)}</Text>
              </TouchableOpacity>
            </View>
            <View style={styles.dateSeparator}>
              <Icon name="arrow-forward" size={16} color={Colors.subtle} />
            </View>
            <View style={styles.dateFlex}>
              <Text style={styles.label}>To</Text>
              <TouchableOpacity style={styles.dateBtn} onPress={() => setPickerField('dateTo')}>
                <Icon name="calendar-today" size={14} color={Colors.blue} style={{marginRight: 6}} />
                <Text style={[styles.dateTxt, !local.dateTo && styles.datePlaceholder]}>{fmt(local.dateTo)}</Text>
              </TouchableOpacity>
            </View>
          </View>

          {showPlant && (
            <>
              <Text style={styles.sectionLabel}>Plant</Text>
              <View style={styles.inputWrap}>
                <Icon name="business" size={16} color={Colors.subtle} style={{marginRight: 8}} />
                <TextInput
                  style={styles.input}
                  value={local.plantId}
                  onChangeText={t => setLocal(v => ({...v, plantId: t}))}
                  placeholder="Plant ID (e.g. Man102)"
                  placeholderTextColor={Colors.placeholder}
                />
              </View>
            </>
          )}

          {showCustomer && (
            <>
              <Text style={styles.sectionLabel}>Customer</Text>
              <View style={styles.inputWrap}>
                <Icon name="person" size={16} color={Colors.subtle} style={{marginRight: 8}} />
                <TextInput
                  style={styles.input}
                  value={local.customerId}
                  onChangeText={t => setLocal(v => ({...v, customerId: t}))}
                  placeholder="Customer ID"
                  placeholderTextColor={Colors.placeholder}
                />
              </View>
            </>
          )}
        </ScrollView>

        {pickerField && (
          <DateTimePicker
            value={local[pickerField] ?? new Date()}
            mode="date"
            display={Platform.OS === 'ios' ? 'spinner' : 'calendar'}
            onChange={handleDateChange}
          />
        )}

        <View style={styles.actions}>
          <TouchableOpacity style={styles.resetBtn} onPress={reset}>
            <Icon name="refresh" size={16} color={Colors.textSecondary} style={{marginRight: 6}} />
            <Text style={styles.resetTxt}>Reset</Text>
          </TouchableOpacity>
          <TouchableOpacity style={styles.applyBtn} onPress={() => { onApply(local); onClose(); }}>
            <Icon name="check" size={16} color="#fff" style={{marginRight: 6}} />
            <Text style={styles.applyTxt}>Apply Filter</Text>
          </TouchableOpacity>
        </View>
      </View>
    </Modal>
  );
}

const styles = StyleSheet.create({
  backdrop: {flex: 1, backgroundColor: 'rgba(10,22,40,0.5)'},
  sheet: {
    backgroundColor: Colors.card,
    borderTopLeftRadius: 24,
    borderTopRightRadius: 24,
    padding: 20,
    paddingBottom: 36,
    maxHeight: '78%',
    shadowColor: Colors.shadow,
    shadowOpacity: 0.2,
    shadowRadius: 24,
    shadowOffset: {width: 0, height: -8},
    elevation: 16,
  },
  handle: {
    width: 36, height: 4, backgroundColor: Colors.border,
    borderRadius: 2, alignSelf: 'center', marginBottom: 14,
  },
  sheetHeader: {
    flexDirection: 'row', justifyContent: 'space-between',
    alignItems: 'center', marginBottom: 20,
  },
  heading: {fontSize: 17, fontWeight: '800', color: Colors.text},
  closeBtn: {
    width: 32, height: 32, borderRadius: 10,
    backgroundColor: Colors.bg, justifyContent: 'center', alignItems: 'center',
  },
  sectionLabel: {
    fontSize: 11, fontWeight: '700', color: Colors.subtle,
    textTransform: 'uppercase', letterSpacing: 0.8,
    marginBottom: 8, marginTop: 16,
  },
  label: {fontSize: 11, color: Colors.subtle, marginBottom: 4, fontWeight: '600'},
  dateRow: {flexDirection: 'row', alignItems: 'flex-end', gap: 8},
  dateFlex: {flex: 1},
  dateSeparator: {paddingBottom: 10},
  dateBtn: {
    flexDirection: 'row', alignItems: 'center',
    borderWidth: 1.5, borderColor: Colors.border, borderRadius: 10,
    paddingHorizontal: 10, paddingVertical: 10, backgroundColor: Colors.bg,
  },
  dateTxt: {fontSize: 13, color: Colors.text, fontWeight: '500'},
  datePlaceholder: {color: Colors.placeholder},
  inputWrap: {
    flexDirection: 'row', alignItems: 'center',
    borderWidth: 1.5, borderColor: Colors.border, borderRadius: 10,
    paddingHorizontal: 12, backgroundColor: Colors.bg,
  },
  input: {flex: 1, paddingVertical: 11, fontSize: 14, color: Colors.text},
  actions: {flexDirection: 'row', gap: 10, marginTop: 24},
  resetBtn: {
    flex: 1, flexDirection: 'row', alignItems: 'center', justifyContent: 'center',
    paddingVertical: 13, borderRadius: 12,
    borderWidth: 1.5, borderColor: Colors.border,
  },
  resetTxt: {fontSize: 14, fontWeight: '600', color: Colors.textSecondary},
  applyBtn: {
    flex: 2, flexDirection: 'row', alignItems: 'center', justifyContent: 'center',
    paddingVertical: 13, borderRadius: 12,
    backgroundColor: Colors.blue,
    shadowColor: Colors.blue, shadowOpacity: 0.35, shadowRadius: 8, shadowOffset: {width: 0, height: 4},
    elevation: 4,
  },
  applyTxt: {fontSize: 14, fontWeight: '700', color: '#fff'},
});
