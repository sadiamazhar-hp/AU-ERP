import React from 'react';
import {StyleSheet, Text, TouchableOpacity, View} from 'react-native';
import Icon from 'react-native-vector-icons/MaterialIcons';
import {Colors} from '../theme/colors';

interface Props {
  icon: string;
  label: string;
  description?: string;
  color?: string;
  badge?: string;
  onPress: () => void;
}

export default function MenuCard({icon, label, description, color = Colors.blue, badge, onPress}: Props) {
  const tint = color + '18';
  return (
    <TouchableOpacity style={styles.card} onPress={onPress} activeOpacity={0.75}>
      <View style={[styles.iconBox, {backgroundColor: tint}]}>
        <Icon name={icon} size={22} color={color} />
      </View>
      <View style={styles.text}>
        <Text style={styles.label}>{label}</Text>
        {description ? <Text style={styles.desc} numberOfLines={1}>{description}</Text> : null}
      </View>
      {badge ? (
        <View style={[styles.badge, {backgroundColor: color + '18'}]}>
          <Text style={[styles.badgeText, {color}]}>{badge}</Text>
        </View>
      ) : null}
      <View style={styles.arrow}>
        <Icon name="chevron-right" size={18} color={Colors.subtle} />
      </View>
    </TouchableOpacity>
  );
}

export function MenuGroup({children}: {children: React.ReactNode}) {
  return <View style={styles.group}>{children}</View>;
}

const styles = StyleSheet.create({
  group: {
    backgroundColor: Colors.card,
    borderRadius: 14,
    overflow: 'hidden',
    shadowColor: Colors.shadow,
    shadowOpacity: 0.07,
    shadowRadius: 8,
    shadowOffset: {width: 0, height: 3},
    elevation: 3,
    marginBottom: 16,
  },
  card: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: Colors.card,
    paddingHorizontal: 16,
    paddingVertical: 15,
    borderBottomWidth: 1,
    borderBottomColor: Colors.divider,
  },
  iconBox: {
    width: 42,
    height: 42,
    borderRadius: 11,
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 14,
  },
  text: {flex: 1, paddingRight: 8},
  label: {fontSize: 15, fontWeight: '600', color: Colors.text, marginBottom: 2},
  desc: {fontSize: 12, color: Colors.subtle, lineHeight: 16},
  badge: {
    paddingHorizontal: 8,
    paddingVertical: 3,
    borderRadius: 10,
    marginRight: 8,
  },
  badgeText: {fontSize: 11, fontWeight: '700'},
  arrow: {
    width: 24,
    alignItems: 'center',
  },
});
