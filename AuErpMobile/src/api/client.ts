import axios from 'axios';
import AsyncStorage from '@react-native-async-storage/async-storage';

export const API_BASE = 'http://localhost:5242/api/mobile'; // Android emulator → localhost:5242

const client = axios.create({
  baseURL: API_BASE,
  timeout: 30_000,
  headers: {'Content-Type': 'application/json'},
});

// Attach stored JWT on every request
client.interceptors.request.use(async config => {
  const token = await AsyncStorage.getItem('jwt_token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export default client;
