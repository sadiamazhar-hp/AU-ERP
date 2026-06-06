import React from 'react';
import {StyleProp, ViewStyle} from 'react-native';
import {SafeAreaView} from 'react-native-safe-area-context';
import {SafeAreaEdgePreset, useScreenSafeEdges} from '../hooks/useOrientationLayout';

interface Props {
  children: React.ReactNode;
  style?: StyleProp<ViewStyle>;
  edgePreset?: SafeAreaEdgePreset;
}

export default function ScreenSafeArea({children, style, edgePreset = 'bottom'}: Props) {
  const edges = useScreenSafeEdges(edgePreset);
  return (
    <SafeAreaView style={style} edges={edges}>
      {children}
    </SafeAreaView>
  );
}
