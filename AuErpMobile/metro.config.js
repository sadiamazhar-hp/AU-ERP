const {getDefaultConfig, mergeConfig} = require('@react-native/metro-config');
const exclusionList = require('metro-config/src/defaults/exclusionList');

/**
 * Block Gradle/CMake output from Metro's watcher (Windows .cxx crash).
 * Use forward slashes only — exclusionList converts them to OS separators.
 */
const config = {
  resolver: {
    blockList: exclusionList([
      /\.cxx\/.*/,
      /android\/app\/build\/.*/,
      /android\/build\/.*/,
      /react-native-reanimated\/.*\/\.cxx\/.*/,
    ]),
  },
};

module.exports = mergeConfig(getDefaultConfig(__dirname), config);
