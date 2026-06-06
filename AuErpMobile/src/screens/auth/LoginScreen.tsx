import React, {useRef, useState} from 'react';
import {
  ActivityIndicator,
  Alert,
  KeyboardAvoidingView,
  Platform,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View,
} from 'react-native';
import Icon from 'react-native-vector-icons/MaterialIcons';
import ScreenSafeArea from '../../components/ScreenSafeArea';
import {useOrientationLayout} from '../../hooks/useOrientationLayout';
import {Colors} from '../../theme/colors';
import {useAuth} from '../../auth/AuthContext';
import {getApiBaseUrl} from '../../config/apiHost';

export default function LoginScreen() {
  const {signIn} = useAuth();
  const {isLandscape} = useOrientationLayout();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPwd, setShowPwd] = useState(false);
  const [loading, setLoading] = useState(false);
  const emailRef = useRef<TextInput>(null);
  const pwdRef = useRef<TextInput>(null);

  function apiErrorMessage(err: unknown): string {
    const e = err as {response?: {data?: {message?: string; Message?: string}}; message?: string};
    const apiMsg = e.response?.data?.message ?? e.response?.data?.Message;
    if (apiMsg) return apiMsg;
    if (e.message === 'Network Error') {
      return (
        `Cannot reach the API at ${getApiBaseUrl()}.\n\n` +
        '• Start AU_ERP: dotnet run --launch-profile http\n' +
        '• Physical phone (USB): adb reverse tcp:5242 tcp:5242\n' +
        '• Wi‑Fi only: set DEV_HOST_OVERRIDE in src/config/apiHost.ts to your PC IPv4'
      );
    }
    if (err instanceof Error && err.message) return err.message;
    return 'Login failed. Check your email and password.';
  }

  async function handleLogin() {
    if (!email.trim() || !password.trim()) {
      Alert.alert('Required', 'Please enter your email and password.');
      return;
    }
    setLoading(true);
    try {
      await signIn({email: email.trim(), password});
    } catch (err: unknown) {
      Alert.alert('Login Failed', apiErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }

  return (
    <ScreenSafeArea style={styles.safe} edgePreset="full">
      <KeyboardAvoidingView behavior={Platform.OS === 'ios' ? 'padding' : undefined} style={{flex: 1}}>
        <ScrollView
          contentContainerStyle={[styles.scroll, isLandscape && styles.scrollLandscape]}
          keyboardShouldPersistTaps="handled">

          {/* ── Hero ── */}
          <View style={[styles.hero, isLandscape && styles.heroLandscape]}>
            {/* Decorative circles */}
            <View style={styles.circle1} />
            <View style={styles.circle2} />
            <View style={styles.circle3} />

            {/* Logo */}
            <View style={styles.logoRing}>
              <View style={styles.logoBox}>
                <Text style={styles.logoText}>AU</Text>
              </View>
            </View>

            <Text style={styles.appName}>AU ERP</Text>
            <Text style={styles.tagline}>Enterprise Management Platform</Text>

            {/* Feature pills */}
            <View style={styles.pillRow}>
              {['Production', 'Sales', 'Inventory'].map(p => (
                <View key={p} style={styles.pill}>
                  <Text style={styles.pillText}>{p}</Text>
                </View>
              ))}
            </View>
          </View>

          {/* ── Form Card ── */}
          <View style={[styles.card, isLandscape && styles.cardLandscape]}>
            <Text style={styles.cardTitle}>Welcome back</Text>
            <Text style={styles.cardSub}>Sign in to your account to continue</Text>

            {/* Email */}
            <Pressable style={styles.fieldWrap} onPress={() => emailRef.current?.focus()}>
              <Icon name="alternate-email" size={18} color={Colors.subtle} style={styles.inputIcon} />
              <TextInput
                ref={emailRef}
                style={styles.input}
                value={email}
                onChangeText={setEmail}
                editable={!loading}
                autoCapitalize="none"
                autoCorrect={false}
                autoComplete="email"
                textContentType="emailAddress"
                keyboardType="email-address"
                returnKeyType="next"
                placeholder="Email address"
                placeholderTextColor={Colors.placeholder}
                onSubmitEditing={() => pwdRef.current?.focus()}
              />
            </Pressable>

            {/* Password */}
            <Pressable style={styles.fieldWrap} onPress={() => pwdRef.current?.focus()}>
              <Icon name="lock-outline" size={18} color={Colors.subtle} style={styles.inputIcon} />
              <TextInput
                ref={pwdRef}
                style={[styles.input, styles.inputWithTrailingIcon]}
                value={password}
                onChangeText={setPassword}
                editable={!loading}
                autoCapitalize="none"
                autoCorrect={false}
                autoComplete="password"
                textContentType="password"
                secureTextEntry={!showPwd}
                returnKeyType="done"
                onSubmitEditing={handleLogin}
                placeholder="Password"
                placeholderTextColor={Colors.placeholder}
              />
              <TouchableOpacity
                style={styles.eyeBtn}
                onPress={() => setShowPwd(v => !v)}
                hitSlop={{top: 12, bottom: 12, left: 12, right: 12}}>
                <Icon name={showPwd ? 'visibility-off' : 'visibility'} size={18} color={Colors.subtle} />
              </TouchableOpacity>
            </Pressable>

            <TouchableOpacity
              style={[styles.loginBtn, loading && styles.loginBtnDisabled]}
              onPress={handleLogin}
              disabled={loading}
              activeOpacity={0.88}>
              {loading ? (
                <ActivityIndicator color="#fff" />
              ) : (
                <>
                  <Text style={styles.loginBtnText}>Sign In</Text>
                  <Icon name="arrow-forward" size={18} color="#fff" style={{marginLeft: 8}} />
                </>
              )}
            </TouchableOpacity>
          </View>

          <Text style={styles.footer}>AU ERP · Read-only reporting access</Text>
        </ScrollView>
      </KeyboardAvoidingView>
    </ScreenSafeArea>
  );
}

const styles = StyleSheet.create({
  safe: {flex: 1, backgroundColor: Colors.heroTop},
  scroll: {flexGrow: 1},
  scrollLandscape: {justifyContent: 'center', minHeight: '100%'},

  // Hero
  hero: {
    backgroundColor: Colors.heroTop,
    paddingTop: 56,
    paddingBottom: 48,
    alignItems: 'center',
    overflow: 'hidden',
    position: 'relative',
  },
  heroLandscape: {
    paddingTop: 24,
    paddingBottom: 20,
  },
  circle1: {
    position: 'absolute', width: 280, height: 280, borderRadius: 140,
    backgroundColor: Colors.blue + '0d', top: -80, right: -80,
  },
  circle2: {
    position: 'absolute', width: 200, height: 200, borderRadius: 100,
    backgroundColor: Colors.blue + '0a', bottom: 0, left: -60,
  },
  circle3: {
    position: 'absolute', width: 120, height: 120, borderRadius: 60,
    backgroundColor: '#ffffff08', top: 20, left: 40,
  },
  logoRing: {
    width: 88, height: 88, borderRadius: 24,
    borderWidth: 1, borderColor: 'rgba(255,255,255,0.12)',
    justifyContent: 'center', alignItems: 'center',
    marginBottom: 20, backgroundColor: 'rgba(255,255,255,0.05)',
  },
  logoBox: {
    width: 64, height: 64, borderRadius: 18,
    backgroundColor: Colors.loginBtn,
    justifyContent: 'center', alignItems: 'center',
    shadowColor: Colors.blue, shadowOpacity: 0.5, shadowRadius: 12, shadowOffset: {width: 0, height: 6},
    elevation: 8,
  },
  logoText: {fontSize: 26, fontWeight: '900', color: '#fff', letterSpacing: 1},
  appName: {fontSize: 28, fontWeight: '800', color: '#fff', letterSpacing: 0.5, marginBottom: 6},
  tagline: {fontSize: 13, color: 'rgba(255,255,255,0.5)', marginBottom: 20},
  pillRow: {flexDirection: 'row', gap: 8},
  pill: {
    backgroundColor: 'rgba(255,255,255,0.1)',
    borderRadius: 20, paddingHorizontal: 12, paddingVertical: 4,
    borderWidth: 1, borderColor: 'rgba(255,255,255,0.15)',
  },
  pillText: {fontSize: 11, color: 'rgba(255,255,255,0.7)', fontWeight: '600'},

  // Card
  card: {
    backgroundColor: Colors.card,
    marginHorizontal: 16,
    borderRadius: 20,
    padding: 24,
    shadowColor: Colors.shadow,
    shadowOpacity: 0.18,
    shadowRadius: 20,
    shadowOffset: {width: 0, height: 8},
    elevation: 8,
  },
  cardLandscape: {
    marginHorizontal: 24,
    maxWidth: 480,
    alignSelf: 'center',
    width: '100%',
  },
  cardTitle: {fontSize: 22, fontWeight: '800', color: Colors.text, marginBottom: 4},
  cardSub: {fontSize: 14, color: Colors.subtle, marginBottom: 24},

  // Fields
  fieldWrap: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: Colors.bg,
    borderWidth: 1.5,
    borderColor: Colors.border,
    borderRadius: 12,
    marginBottom: 14,
    minHeight: 52,
    paddingRight: 12,
  },
  inputIcon: {marginLeft: 14, marginRight: 8},
  input: {
    flex: 1,
    paddingVertical: 14,
    paddingRight: 8,
    fontSize: 15,
    color: Colors.text,
  },
  inputWithTrailingIcon: {paddingRight: 40},
  eyeBtn: {position: 'absolute', right: 14, padding: 4},

  // Button
  loginBtn: {
    backgroundColor: Colors.loginBtn,
    borderRadius: 12,
    paddingVertical: 15,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    marginTop: 8,
    shadowColor: Colors.loginBtn,
    shadowOpacity: 0.4,
    shadowRadius: 12,
    shadowOffset: {width: 0, height: 6},
    elevation: 6,
  },
  loginBtnDisabled: {opacity: 0.65},
  loginBtnText: {color: '#fff', fontSize: 16, fontWeight: '700'},

  footer: {
    textAlign: 'center',
    color: 'rgba(255,255,255,0.25)',
    fontSize: 12,
    paddingVertical: 28,
  },
});
