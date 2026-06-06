import {useMemo} from 'react';
import {useWindowDimensions} from 'react-native';
import type {Edge} from 'react-native-safe-area-context';

export const BASE_TABLE_COL_WIDTH = 92;
const SCREEN_HORIZONTAL_PADDING = 64;

export type SafeAreaEdgePreset = 'top' | 'bottom' | 'full';

export function useOrientationLayout() {
  const {width, height} = useWindowDimensions();

  return useMemo(() => {
    const isLandscape = width > height;
    const columnsPerRow = isLandscape ? (width >= 720 ? 4 : 3) : 2;
    const baseColWidth = isLandscape
      ? Math.round(BASE_TABLE_COL_WIDTH * 1.15)
      : BASE_TABLE_COL_WIDTH;
    const contentWidth = Math.max(width - SCREEN_HORIZONTAL_PADDING, 280);

    return {
      width,
      height,
      isLandscape,
      columnsPerRow,
      baseColWidth,
      chartContentWidth: contentWidth,
      tableContentWidth: contentWidth,
    };
  }, [width, height]);
}

export function useScreenSafeEdges(preset: SafeAreaEdgePreset = 'bottom'): Edge[] {
  const {isLandscape} = useOrientationLayout();

  if (preset === 'top') {
    return isLandscape ? ['top', 'left', 'right'] : ['top'];
  }
  if (preset === 'full') {
    return isLandscape ? ['top', 'bottom', 'left', 'right'] : ['top', 'bottom'];
  }
  return isLandscape ? ['bottom', 'left', 'right'] : ['bottom'];
}
