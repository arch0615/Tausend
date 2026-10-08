package com.alarmastausend.tausend

import android.os.Bundle
import com.facebook.react.ReactActivity
import com.facebook.react.ReactActivityDelegate
import com.facebook.react.defaults.DefaultNewArchitectureEntryPoint.fabricEnabled
import com.facebook.react.defaults.DefaultReactActivityDelegate

class MainActivity : ReactActivity() {

  /**
   * Returns the name of the main component registered from JavaScript. This is used to schedule
   * rendering of the component.
   */
  override fun getMainComponentName(): String = "TausendMobile"

  /**
   * Returns the instance of the [ReactActivityDelegate]. We use [DefaultReactActivityDelegate]
   * which allows you to enable New Architecture with a single boolean flags [fabricEnabled]
   */
  override fun createReactActivityDelegate(): ReactActivityDelegate =
      DefaultReactActivityDelegate(this, mainComponentName, fabricEnabled)

  /**
   * Deliberately passes null instead of [savedInstanceState], which react-native-screens
   * requires of any activity hosting its screens.
   *
   * Android normally hands the saved Fragment state back on recreation and rebuilds those
   * Fragments itself. react-native-screens owns its Fragments outright and rejects that: its
   * ScreenFragment constructor throws "Screen fragments should never be restored", which takes
   * the whole app down before the first frame. React Native rebuilds the entire UI from the JS
   * side anyway, so there is no Android-side view state worth restoring here.
   *
   * Without this the app crashes on every activity recreation: rotating the device, a
   * system-driven configuration change (locale, theme, font size), or coming back to the app
   * after Android has killed it in the background to reclaim memory. Found by launching the
   * release build on an emulator; the process died on start with exactly that exception.
   *
   * See https://github.com/software-mansion/react-native-screens#android (the ReactActivity
   * setup note) and the issue the exception message itself points at.
   */
  override fun onCreate(savedInstanceState: Bundle?) {
    super.onCreate(null)
  }
}
