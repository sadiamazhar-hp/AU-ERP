import React from 'react';
import {ActivityIndicator, StyleSheet, Text, View} from 'react-native';
import {Colors} from '../theme/colors';

export default function LoadingView({message = 'Loading…'}: {message?: string}) {
  return (
    <View style={styles.container}>
      <View style={styles.logoBox}>
        <Text style={styles.logoText}>AU</Text>
      </View>
      <ActivityIndicator size="large" color={Colors.blue} style={styles.spinner} />
      <Text style={styles.text}>{message}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {flex: 1, justifyContent: 'center', alignItems: 'center', backgroundColor: Colors.bg},
  logoBox: {
    width: 52,
    height: 52,
    borderRadius: 14,
    backgroundColor: Colors.shell,
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 20,
  },
  logoText: {fontSize: 20, fontWeight: '800', color: '#fff'},
  spinner: {marginBottom: 12},
  text: {fontSize: 14, color: Colors.subtle, fontWeight: '500'},
});
