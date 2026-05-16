import React, {createContext, useContext, useEffect, useState} from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';
import {login as apiLogin, LoginRequest, LoginResponse} from '../api/auth';

interface AuthState {
  token: string | null;
  user: LoginResponse | null;
  isLoading: boolean;
}

interface AuthContextValue extends AuthState {
  signIn: (req: LoginRequest) => Promise<void>;
  signOut: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({children}: {children: React.ReactNode}) {
  const [state, setState] = useState<AuthState>({token: null, user: null, isLoading: true});

  useEffect(() => {
    (async () => {
      const token = await AsyncStorage.getItem('jwt_token');
      const userJson = await AsyncStorage.getItem('jwt_user');
      if (token && userJson) {
        setState({token, user: JSON.parse(userJson), isLoading: false});
      } else {
        setState(s => ({...s, isLoading: false}));
      }
    })();
  }, []);

  const signIn = async (req: LoginRequest) => {
    const data = await apiLogin(req);
    await AsyncStorage.setItem('jwt_token', data.token);
    await AsyncStorage.setItem('jwt_user', JSON.stringify(data));
    setState({token: data.token, user: data, isLoading: false});
  };

  const signOut = async () => {
    await AsyncStorage.removeItem('jwt_token');
    await AsyncStorage.removeItem('jwt_user');
    setState({token: null, user: null, isLoading: false});
  };

  return (
    <AuthContext.Provider value={{...state, signIn, signOut}}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider');
  return ctx;
}
