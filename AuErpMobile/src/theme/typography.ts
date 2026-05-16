import {StyleSheet} from 'react-native';
import {Colors} from './colors';

export const Typography = StyleSheet.create({
  h1: {fontSize: 24, fontWeight: '700', color: Colors.text, letterSpacing: -0.5},
  h2: {fontSize: 20, fontWeight: '700', color: Colors.text},
  h3: {fontSize: 16, fontWeight: '600', color: Colors.text},
  body: {fontSize: 14, fontWeight: '400', color: Colors.text, lineHeight: 20},
  bodySmall: {fontSize: 12, fontWeight: '400', color: Colors.subtle, lineHeight: 16},
  label: {fontSize: 11, fontWeight: '600', color: Colors.subtle, letterSpacing: 0.5, textTransform: 'uppercase'},
  mono: {fontSize: 13, fontFamily: 'monospace', color: Colors.text},
});
