import React, {useEffect, useState} from 'react';
import {StyleSheet, TextInput, View} from 'react-native';
import Icon from 'react-native-vector-icons/MaterialIcons';
import {Colors} from '../theme/colors';

interface Props {
  value: string;
  onChangeText: (text: string) => void;
  placeholder?: string;
}

export default function SearchBar({value, onChangeText, placeholder = 'Search table…'}: Props) {
  const [local, setLocal] = useState(value);

  useEffect(() => {
    setLocal(value);
  }, [value]);

  useEffect(() => {
    const t = setTimeout(() => onChangeText(local.trim()), 250);
    return () => clearTimeout(t);
  }, [local, onChangeText]);

  return (
    <View style={styles.wrap}>
      <Icon name="search" size={18} color={Colors.subtle} />
      <TextInput
        style={styles.input}
        value={local}
        onChangeText={setLocal}
        placeholder={placeholder}
        placeholderTextColor={Colors.placeholder}
        autoCapitalize="none"
        autoCorrect={false}
        clearButtonMode="while-editing"
      />
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: Colors.card,
    borderWidth: 1,
    borderColor: Colors.border,
    borderRadius: 10,
    paddingHorizontal: 12,
    marginBottom: 10,
    gap: 8,
  },
  input: {flex: 1, paddingVertical: 10, fontSize: 14, color: Colors.text},
});
