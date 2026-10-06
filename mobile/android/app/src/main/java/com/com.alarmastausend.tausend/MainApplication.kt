package com.alarmastausend.tausend

import android.app.Application
import android.app.NotificationChannel
import android.app.NotificationManager
import android.media.AudioAttributes
import android.net.Uri
import android.os.Build
import com.facebook.react.PackageList
import com.facebook.react.ReactApplication
import com.facebook.react.ReactHost
import com.facebook.react.ReactNativeApplicationEntryPoint.loadReactNative
import com.facebook.react.defaults.DefaultReactHost.getDefaultReactHost

class MainApplication : Application(), ReactApplication {

  override val reactHost: ReactHost by lazy {
    getDefaultReactHost(
      context = applicationContext,
      packageList =
        PackageList(this).packages.apply {
          // Packages that cannot be autolinked yet can be added manually here, for example:
          // add(MyReactNativePackage())
        },
    )
  }

  override fun onCreate() {
    super.onCreate()
    loadReactNative(this)
    createNotificationChannels()
  }

  // Ported from the previous app's MyFirebaseMessagingService.createNotificationChannel() --
  // backend-core's FirebasePushNotificationBusiness.CreatePayload already sends
  // android_channel_id="my_channel_id" + sound="alert" for alarm-type notifications (Panic,
  // Assault, Fire, etc.), matching this exactly. But a channel referenced by a notification that
  // doesn't exist yet on the device gets silently auto-created by Android with NO custom sound,
  // and a channel's sound can never be changed after creation -- only deleting and recreating it
  // works, which is exactly why this must run unconditionally at every app startup, before any
  // notification can possibly arrive, rather than lazily on first message like the old app did.
  private fun createNotificationChannels() {
    if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O) return
    val notificationManager = getSystemService(NotificationManager::class.java)

    val alertSoundUri = Uri.parse("android.resource://" + packageName + "/" + R.raw.alert)
    val alarmChannel = NotificationChannel(
      "my_channel_id",
      "Alarmas",
      NotificationManager.IMPORTANCE_HIGH,
    )
    alarmChannel.description = "Pánico, asalto, incendio y otras alarmas reales"
    alarmChannel.setSound(
      alertSoundUri,
      AudioAttributes.Builder()
        .setUsage(AudioAttributes.USAGE_NOTIFICATION)
        .setContentType(AudioAttributes.CONTENT_TYPE_SONIFICATION)
        .build(),
    )
    notificationManager.createNotificationChannel(alarmChannel)

    val defaultChannel = NotificationChannel(
      "default_channel_id",
      "General",
      NotificationManager.IMPORTANCE_DEFAULT,
    )
    defaultChannel.description = "Notificaciones generales"
    notificationManager.createNotificationChannel(defaultChannel)
  }
}
