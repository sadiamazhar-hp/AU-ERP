import React from 'react';
import {StyleSheet, Text, TouchableOpacity, View} from 'react-native';
import Icon from 'react-native-vector-icons/MaterialIcons';
import {Colors} from '../theme/colors';

interface Props {
  message?: string;
  onRetry?: () => void;
}

export default function ErrorView({message = 'Something went wrong.', onRetry}: Props) {
  return (
    <View style={styles.container}>
      <View style={styles.iconWrap}>
        <Icon name="wifi-off" size={32} color={Colors.red} />
      </View>
      <Text style={styles.title}>Unable to load data</Text>
      <Text style={styles.message}>{message}</Text>
      {onRetry && (
        <TouchableOpacity style={styles.btn} onPress={onRetry} activeOpacity={0.85}>
          <Icon name="refresh" size={16} color="#fff" style={{marginRight: 6}} />
          <Text style={styles.btnText}>Try Again</Text>
        </TouchableOpacity>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {flex: 1, justifyContent: 'center', alignItems: 'center', backgroundColor: Colors.bg, padding: 32},
  iconWrap: {
    width: 68,
    height: 68,
    borderRadius: 20,
    backgroundColor: Colors.redLight,
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 16,
  },
  title: {fontSize: 17, fontWeight: '700', color: Colors.text, marginBottom: 6},
  message: {fontSize: 14, color: Colors.subtle, textAlign: 'center', lineHeight: 20, marginBottom: 24},
  btn: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: Colors.blue,
    paddingHorizontal: 24,
    paddingVertical: 12,
    borderRadius: 10,
    shadowColor: Colors.blue,
    shadowOpacity: 0.3,
    shadowRadius: 8,
    shadowOffset: {width: 0, height: 4},
    elevation: 4,
  },
  btnText: {color: '#fff', fontWeight: '700', fontSize: 14},
});
