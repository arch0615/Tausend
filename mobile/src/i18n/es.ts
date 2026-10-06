// Spanish translations, keyed by the literal English source string (see LocaleContext.tsx's
// identity-key pattern). Terminology matches the previous (Ionic) app's established Spanish UI
// where a direct equivalent exists (e.g. "Central" for panel, "Desarmar" for disarm, "Zona" for
// zone), rather than inventing new phrasing, since that's what the client's users already know.
export const es: Record<string, string> = {
  'Choose the language for this app.': 'Elige el idioma de la aplicación.',

  // MainMenu.tsx / HomeScreen.tsx first-login welcome
  'Version {v}': 'Versión {v}',
  'Access Point': 'Access Point',
  'VINCULAR EQUIPO': 'VINCULAR EQUIPO',
  'No linked panels': 'No existen centrales vinculadas',
  'Connect Access Point': 'Conectar Access Point',
  'Add equipment': 'Añadir Equipo',
  Attention: 'Atención',
  'This will log you out automatically after updating the panel.':
    'Se cerrará sesión automáticamente luego de actualizar el equipo.',
  Updating: 'Actualizando',
  'ALARMAS TAUSEND 2.0': 'ALARMAS TAUSEND 2.0',
  'Your phone': 'Tu teléfono',
  'Currently connected to': 'Conectado actualmente a',
  'No network': 'Sin red',
  'Select a panel': 'Selecciona una central',
  'IP PANEL': 'CENTRAL IP',
  SETUP: 'CONFIGURACIÓN',
  'SMS PANEL': 'CENTRAL SMS',
  ACCOUNT: 'CUENTA',
  Connected: 'Conectada',
  'Not connected': 'Sin conexión',

  // Navigation titles (MainStack.tsx)
  'Change password': 'Cambiar contraseña',
  'Add a panel': 'Agregar una central',
  'Vincular Equipo': 'Vincular Equipo',
  'Your panels': 'Tus centrales',
  'Device selector': 'Selector de Equipo',
  'Manage panel': 'Gestionar central',
  'Identify panel': 'Identificar central',
  Zones: 'Zonas',
  Exclusions: 'Exclusiones',
  Memory: 'Memoria',
  'PGM outputs': 'Salidas Programables',
  'SALIDAS PROGRAMABLES': 'SALIDAS PROGRAMABLES',
  'Change name': 'Cambiar nombre',
  Name: 'Nombre',
  'Change saved successfully': 'Cambio guardado correctamente',
  Ok: 'Ok',
  'Scheduled departures': 'Salidas programadas',
  'User labels': 'Etiquetas de usuarios',
  'ETIQUETAS DE USUARIO': 'ETIQUETAS DE USUARIO',
  Battery: 'Batería',
  Failures: 'Fallas',
  FALLAS: 'FALLAS',
  Events: 'Eventos',
  EVENTOS: 'EVENTOS',
  'Custom messages': 'Mensajes personalizados',
  'Contact numbers': 'Números de contacto',
  Clock: 'Puesta en Hora',
  'PUESTA EN HORA': 'PUESTA EN HORA',
  'Installer mode': 'Instalador',
  INSTALADOR: 'INSTALADOR',
  'Panel settings': 'Ajustes de la central',
  Language: 'Idioma',
  'User account': 'Cuenta de usuario',
  'Delete account': 'Eliminar cuenta',
  'Delete account?': '¿Eliminar cuenta?',
  'This permanently removes your account and unlinks your panel. This cannot be undone.':
    'Esto elimina tu cuenta de forma permanente y desvincula tu central. Esta acción no se puede deshacer.',

  // HomeScreen.tsx
  Home: 'Inicio',
  Loading: 'Cargando',
  'Please wait': 'Aguarde',
  "You don't have any panels linked yet.": 'Todavía no tienes ninguna central vinculada.',
  'Select a panel to get started.': 'Selecciona una central para comenzar.',
  'Could not reach the server.': 'No se pudo conectar con el servidor.',
  "The panel isn't responding -- it may be offline.": 'La central no responde: puede estar sin conexión.',
  'The panel rejected that PIN or value.': 'La central rechazó ese PIN o valor.',
  'The panel could not complete that action right now.': 'La central no pudo completar esa acción en este momento.',
  'Your account is not authorized for that action.': 'Tu cuenta no está autorizada para esa acción.',
  'This panel could not be found.': 'No se encontró esta central.',
  'Something went wrong. Try again.': 'Algo salió mal. Intenta de nuevo.',
  'The panel took too long to respond. Try again.': 'La central tardó demasiado en responder. Intenta de nuevo.',
  'Send a panic alert? This notifies your emergency contacts immediately.':
    '¿Enviar una alerta de pánico? Esto notifica a tus contactos de emergencia de inmediato.',
  'Send a duress alert? Only use this under threat or coercion.':
    '¿Enviar una alerta de coacción? Úsala solo bajo amenaza o coacción.',
  'Send a medical emergency alert? This notifies your emergency contacts immediately.':
    '¿Enviar una alerta de emergencia médica? Esto notifica a tus contactos de emergencia de inmediato.',
  'Panic alert sent': 'Alerta de pánico enviada',
  'Duress alert sent': 'Alerta de coacción enviada',
  'Emergency alert sent': 'Alerta de emergencia enviada',
  'Panic alert failed': 'Falló la alerta de pánico',
  'Duress alert failed': 'Falló la alerta de coacción',
  'Emergency alert failed': 'Falló la alerta de emergencia',
  'The panel confirmed receipt.': 'La central confirmó la recepción.',
  'Disarm the alarm?': '¿Desarmar la alarma?',
  Cancel: 'Cancelar',
  Back: 'Volver',
  OK: 'Aceptar',
  Show: 'Mostrar',
  Hide: 'Ocultar',
  Emergency: 'Emergencia',
  Panic: 'Pánico',
  Duress: 'Coacción',
  'Zones excluded': 'Exclusiones',
  Fault: 'Fallas',
  "The panel isn't responding. Pull to refresh or tap below to try again.":
    'La central no responde. Desliza para actualizar o toca abajo para intentar de nuevo.',
  Refresh: 'Actualizar',
  'Arm mode': 'Modo de armado',
  Away: 'Ausente',
  Day: 'Día',
  Night: 'Noche',
  'Mode: {mode}': 'Modo: {mode}',
  'Siren active': 'Sirena activa',
  Armed: 'Armada',
  'Ready to arm': 'Lista para armar',
  'Not ready (zones open)': 'No lista (zonas abiertas)',
  'Command error': 'Error de comando',
  'No response': 'Sin respuesta',
  'Day mode': 'Modo Día',
  'Night mode': 'Modo Noche',
  'Away mode': 'Modo Ausente',
  // Arm/disarm confirmation toast (ported from home.page.ts's getStatusMsg) -- deliberately
  // distinct wording from the status card's own labels above (e.g. card: "Modo Día", toast:
  // "Armada (modo día)") since the toast always prefixes the mode with "Armed".
  'Command sent successfully': 'Comando enviado correctamente',
  'Not ready, zones open': 'Desarmada pero no lista (zonas abiertas)',
  'Armed (day mode)': 'Armada (modo día)',
  'Armed (night mode)': 'Armada (modo noche)',
  'Armed (away mode)': 'Armada (modo ausente)',
  'Armed with no entry delay': 'Armada sin demora en entrada',
  'Siren sounding': 'Sirena sonando',
  'Could not open your messaging app.': 'No se pudo abrir tu aplicación de mensajes.',
  'SMS panel': 'Central SMS',
  "SMS panels aren't monitored live -- each action sends a text message from your own phone, one confirmation tap away.":
    'Las centrales SMS no se monitorean en vivo: cada acción envía un mensaje de texto desde tu propio teléfono, a solo un toque de confirmación.',
  'Check status': 'Consultar estado',
  'Arm away': 'Armar ausente',
  'Arm day': 'Armar día',
  'Arm night': 'Armar noche',
  Disarm: 'Desarmar',
  'Switch panel': 'Cambiar de central',
  'Log out': 'Cerrar sesión',

  // Auth screens (Login, Register, ForgotPassword, ChangePassword)
  'Enter your email and password.': 'Ingresa tu correo electrónico y contraseña.',
  'Sign in to your account': 'Inicia sesión en tu cuenta',
  Email: 'Correo electrónico',
  Password: 'Contraseña',
  'Log in': 'Iniciar sesión',
  'The PIN used for panel "{name}" is no longer valid. Please link it again.':
    'El código de usuario de la central "{name}" ya no es válido. Volvé a vincularla.',
  'The PIN used for panels {names} is no longer valid. Please link them again.':
    'El código de usuario de las centrales {names} ya no es válido. Volvé a vincularlas.',
  'Forgot password?': '¿Olvidaste tu contraseña?',
  'Create account': 'Crear cuenta',
  'Fill in every field.': 'Completa todos los campos.',
  'Password must be at least 8 characters.': 'La contraseña debe tener al menos 8 caracteres.',
  'Passwords do not match.': 'Las contraseñas no coinciden.',
  'Could not create the account.': 'No se pudo crear la cuenta.',
  'Account created. Log in to continue.': 'Cuenta creada. Inicia sesión para continuar.',
  'Set up access to your alarm panels': 'Configura el acceso a tus centrales de alarma',
  'First name': 'Nombre',
  'Last name': 'Apellido',
  'Confirm password': 'Confirmar contraseña',
  'Back to login': 'Volver a iniciar sesión',
  'Enter your email.': 'Ingresa tu correo electrónico.',
  'Forgot password': 'Olvidé mi contraseña',
  "We'll email you a link to reset it.": 'Te enviaremos un enlace por correo para restablecerla.',
  'If that email is registered, a reset link is on its way. Check your inbox.':
    'Si ese correo está registrado, un enlace de restablecimiento está en camino. Revisa tu bandeja de entrada.',
  'Send reset link': 'Enviar enlace de restablecimiento',
  'New password must be at least 8 characters.': 'La nueva contraseña debe tener al menos 8 caracteres.',
  'New passwords do not match.': 'Las nuevas contraseñas no coinciden.',
  'New password must be different from your current password.':
    'La nueva contraseña debe ser diferente a la actual.',
  'Change password?': '¿Cambiar contraseña?',
  "You'll be logged out automatically afterward.": 'Se cerrará sesión automáticamente después.',
  'Could not change the password.': 'No se pudo cambiar la contraseña.',
  'Current password': 'Contraseña actual',
  'New password': 'Nueva contraseña',
  'Confirm new password': 'Confirmar nueva contraseña',
  Save: 'Guardar',

  // Pairing flow
  'How does your panel connect?': '¿Cómo se conecta tu central?',
  'Wi-Fi panel': 'Central Wi-Fi',
  "Wi-Fi panels connect to your home network and are controlled over the internet. SMS panels have no Wi-Fi and are controlled by text message to a SIM card in the panel.":
    'Las centrales Wi-Fi se conectan a tu red doméstica y se controlan por internet. Las centrales SMS no tienen Wi-Fi y se controlan por mensaje de texto a una tarjeta SIM en la central.',
  "Before you start, put your alarm panel into Access Point (setup) mode -- check the panel's manual for the exact button or installer-code sequence for your model.":
    'Antes de comenzar, pon tu central de alarma en modo Punto de Acceso (configuración); consulta el manual de la central para conocer el botón exacto o la secuencia de código de instalador para tu modelo.',
  "Once it's in setup mode, the panel will broadcast its own Wi-Fi network. You'll connect to that network briefly to check and/or modify its programming.":
    'Una vez en modo configuración, la central transmitirá su propia red Wi-Fi. Te conectarás brevemente a esa red para consultar y/o modificar la programación.',
  "I'm ready": 'Estoy listo',
  'Connect to the panel': 'Conectar con la central',
  "Pick the panel's network below, or enter its name manually.": 'Elige la red de la central abajo, o ingresa su nombre manualmente.',
  Connect: 'Conectar',
  Confirm: 'Confirmar',
  'ACCESS POINT': 'ACCESS POINT',
  'Put the panel into Access Point mode, find its network, and connect.':
    'Poner el equipo en Access Point, buscar la red del equipo y conectarse.',
  'Once your phone is connected to the panel network, connect the app to the panel.':
    'Una vez conectado éste dispositivo a la red del equipo conecte la App al equipo.',
  'Remote IP address': 'Dirección IP Remota',
  'Remote port': 'Puerto Remoto',
  'Network selector': 'SELECTOR DE RED',
  'No networks available.': 'No se encontraron redes disponibles.',
  'Connect to {ssid}': 'Conectar a {ssid}',
  'Connected to {ssid}': 'Conectado a {ssid}',
  'If you changed the installer code, replace the default value.':
    'Si Ud. cambió el código de instalador cambie el valor por defecto.',
  'Configure network name and password': 'Configurar nombre de red y contraseña',
  'Network name - PRG350': 'Nombre de red - PRG350:',
  'Password - PRG351': 'Password - PRG351:',
  'Please fill in both fields.': 'Por favor complete los campos.',
  Finish: 'Finalizar',
  'Finish Access Point': 'Finalizar Access Point',
  "To finish, take the panel out of Access Point mode. Wait a few seconds for it to connect to the server -- once it does, you can link the device to your account. If it doesn't connect, put the panel back into Access Point mode and double-check the network name and password.":
    "Para finalizar sacar el equipo de Access Point. Espere unos segundos hasta que el equipo se conecte al servidor. En ese caso puede vincular el dispositivo a su cuenta. En el caso que no se conecte, vuelva a poner el equipo en Access Point y verifique el nombre de red y contraseña.",
  "Connect to the panel's network from your phone's Wi-Fi settings.":
    'Conéctese a la red del equipo desde la configuración Wi-Fi del teléfono.',
  'Location permission is required to scan for Wi-Fi networks.': 'Se requiere permiso de ubicación para buscar redes Wi-Fi.',
  'Could not scan for Wi-Fi networks.': 'No se pudieron buscar redes Wi-Fi.',
  'Incorrect password for that network -- check the installer code and try again.':
    'Contraseña incorrecta para esa red; verifica el código de instalador e intenta de nuevo.',
  "Couldn't find that network. Make sure the panel is still in setup mode and try again.":
    'No se encontró esa red. Asegúrate de que la central siga en modo configuración e intenta de nuevo.',
  'Connection timed out. Move closer to the panel and try again.': 'Se agotó el tiempo de conexión. Acércate a la central e intenta de nuevo.',
  'Location permission is required to connect to a Wi-Fi network.': 'Se requiere permiso de ubicación para conectarse a una red Wi-Fi.',
  'Could not connect to that network. Try again.': 'No se pudo conectar a esa red. Intenta de nuevo.',
  Continue: 'Continuar',
  "Couldn't reach the panel. Make sure your phone is still connected to its network and try again.":
    'No se pudo contactar al equipo. Asegúrate de que tu teléfono siga conectado a su red e intenta de nuevo.',
  'Connecting to the panel timed out. Move closer to it and try again.': 'Se agotó el tiempo de conexión con el equipo. Acércate a él e intenta de nuevo.',
  'Lost the connection to the panel.': 'Se perdió la conexión con el equipo.',
  'Could not send the command to the panel.': 'No se pudo enviar el comando al equipo.',

  // CreateDeviceScreen.tsx
  'Link this panel': 'Vincular esta central',
  "Enter the panel's identifier and its 4-digit user PIN to add it to your account.":
    'Ingresa el identificador de la central y un código de usuario dado de alta previamente en la central para agregarla a tu cuenta.',
  "Enter the panel's identifier.": 'Ingresa el identificador de la central.',
  'The PIN must be 4 digits.': 'El PIN debe tener 4 dígitos.',
  'Too many incorrect attempts. Double-check the panel details before trying again.':
    'Demasiados intentos incorrectos. Verifica los datos de la central antes de intentar de nuevo.',
  'Incorrect PIN. {left} attempt left.': 'PIN incorrecto. Queda {left} intento.',
  'Incorrect PIN. {left} attempts left.': 'PIN incorrecto. Quedan {left} intentos.',
  'Panel name': 'Descripción',
  'Could not link the panel.': 'No se pudo vincular la central.',
  "Too many incorrect PIN attempts. Go back and confirm the panel's identifier and PIN before trying again.":
    'Demasiados intentos de PIN incorrecto. Regresa y confirma el identificador y el PIN de la central antes de intentar de nuevo.',
  'Panel identifier': 'Identificador',
  'User code (4 digits)': 'Código de Usuario',
  'Panel description': 'Descripción',

  // SmsPairingScreen.tsx
  'Add an SMS panel': 'Agregar una central SMS',
  'Enter the phone number of the SIM card installed in the panel, and its SMS PIN (set by the installer).':
    'Ingresa el número de teléfono de la tarjeta SIM instalada en la central, y su PIN de SMS (configurado por el instalador).',
  'Panel model': 'Modelo de la central',
  'Select your panel model.': 'Selecciona el modelo de tu central.',
  'Enter a name for this panel.': 'Ingresa un nombre para esta central.',
  'Enter a valid phone number for the panel’s SIM card.':
    'Ingresa un número de teléfono válido para la tarjeta SIM de la central.',
  'The SIM PIN must be 4 digits.': 'El PIN de SIM debe tener 4 dígitos.',
  'You already have a panel with that name.': 'Ya tienes una central con ese nombre.',
  'Could not register the panel.': 'No se pudo registrar la central.',
  'SIM phone number': 'Número de teléfono',
  'SIM PIN (4 digits)': 'Clave SMS',

  // PairingConfirmationScreen.tsx
  'Panel added': 'Central agregada',
  'Your panel has been linked to your account.': 'Tu central ha sido vinculada a tu cuenta.',
  'Your SMS panel has been registered.': 'Tu central SMS ha sido registrada.',
  "It should come online within a minute or two. If it doesn't, put it back into setup mode and double-check the Wi-Fi name and password you gave it.":
    'Debería conectarse en uno o dos minutos. Si no lo hace, vuelve a ponerla en modo configuración y verifica el nombre y la contraseña de Wi-Fi que ingresaste.',
  "You can now control it by text message. Commands are sent from your phone's own messaging app, one confirmation tap at a time.":
    'Ahora puedes controlarla por mensaje de texto. Los comandos se envían desde la propia aplicación de mensajes de tu teléfono, con un toque de confirmación cada vez.',
  "For security, you'll be logged out now -- log back in to see your new panel.":
    'Por seguridad, se cerrará la sesión ahora; vuelve a iniciar sesión para ver tu nueva central.',
  Done: 'Listo',

  // ZonesScreen.tsx
  "The panel isn't responding right now.": 'La central no responde en este momento.',
  'Unsaved changes': 'Cambios sin guardar',
  'You have unsaved zone names. Leave without saving?': 'Tienes nombres de zonas sin guardar. ¿Salir sin guardar?',
  Stay: 'Quedarse',
  Discard: 'Descartar',
  "The panel isn't responding right now. Try again.": 'La central no responde en este momento. Intenta de nuevo.',
  'Could not save changes.': 'No se pudieron guardar los cambios.',
  'Select a Wi-Fi panel to manage its zones.': 'Selecciona una central Wi-Fi para gestionar sus zonas.',
  'No zones yet.': 'Todavía no hay zonas.',
  'Zone {n}': 'Zona {n}',
  Edit: 'Editar',
  'Save changes': 'Guardar Cambios',

  // ExclusionsScreen.tsx
  "Zones can't be excluded while the alarm is armed.": 'Las zonas no se pueden excluir mientras la alarma está armada.',
  'Select a Wi-Fi panel to manage its exclusions.': 'Selecciona una central Wi-Fi para gestionar sus exclusiones.',
  'The alarm is armed -- you can only remove existing exclusions.':
    'La alarma está armada; solo puedes quitar exclusiones existentes.',

  // MemoryScreen.tsx
  'Select a Wi-Fi panel to view its memory.': 'Selecciona una central Wi-Fi para ver su memoria.',
  'No zones currently have memory.': 'Ninguna zona tiene memoria actualmente.',

  // ProgramControlList.tsx (shared by PgmScreen/ScheduledDeparturesScreen)
  Output: 'Salida',
  'Output {n}': 'Salida {n}',
  'Select an SMS panel to manage its outputs.': 'Selecciona una central SMS para gestionar sus salidas.',
  'Toggle an output on "{name}" by text message.': 'Activa o desactiva una salida en "{name}" por mensaje de texto.',

  // SmsPasswordChangeScreen.tsx
  'Change SMS PIN': 'Cambiar PIN SMS',
  'Change the SMS PIN on "{name}" by text message.': 'Cambia el PIN SMS de "{name}" por mensaje de texto.',
  'Select an SMS panel to change its PIN.': 'Selecciona una central SMS para cambiar su PIN.',
  'SMS PIN': 'PIN SMS',
  'Repeat SMS PIN': 'Repetir PIN SMS',
  'Enter a valid SMS PIN.': 'Ingresa un PIN SMS válido.',
  'Enter a valid confirmation PIN.': 'Ingresa un PIN de confirmación válido.',
  'The PINs do not match.': 'Los PIN no coinciden.',
  'Was the SMS PIN changed successfully on the panel?': '¿Se cambió correctamente el PIN SMS en la central?',
  'Could not update the panel.': 'No se pudo actualizar la central.',
  Yes: 'Sí',
  No: 'No',
  Outputs: 'Salidas',
  Departure: 'Partida',
  Departures: 'Partidas',
  'You have unsaved {name} names. Leave without saving?': 'Tienes nombres de {name} sin guardar. ¿Salir sin guardar?',
  'Could not toggle the output.': 'No se pudo cambiar el estado de la salida.',
  'Select a Wi-Fi panel to manage its {name}.': 'Selecciona una central Wi-Fi para gestionar sus {name}.',
  'No {name} yet.': 'Todavía no hay {name}.',

  // PanelUsersScreen.tsx
  'Could not load user labels.': 'No se pudieron cargar las etiquetas de usuarios.',
  'You have unsaved user labels. Leave without saving?': 'Tienes etiquetas de usuarios sin guardar. ¿Salir sin guardar?',
  'Enter a valid PIN-holder number.': 'Ingresa un número de usuario válido.',
  'Enter a label for this PIN-holder.': 'Ingresa una etiqueta para este usuario.',
  'That PIN-holder number already has a label.': 'Ese número de usuario ya tiene una etiqueta.',
  'Select a Wi-Fi panel to manage its user labels.': 'Selecciona una central Wi-Fi para gestionar sus etiquetas de usuarios.',
  'No PIN-holder labels yet.': 'Todavía no hay etiquetas de usuarios.',
  'User number {n}': 'Usuario número {n}',
  'PIN-holder number': 'Número de usuario',
  Label: 'Etiqueta',
  // Ported from the previous app's users-labels.page.ts -- add and edit share one modal (edit
  // just prefills it and locks the user-number field), and tapping a row opens a
  // "Seleccionar acción" choice between deleting or editing its label, rather than this screen's
  // previous inline Edit/Remove text links.
  'Add label': 'Añadir etiqueta de usuario',
  'Edit user label': 'Modificar etiqueta de usuario',
  'Select action': 'Seleccionar acción',
  'Delete label': 'Eliminar etiqueta',
  'Edit label': 'Modificar etiqueta',

  // BatteryScreen.tsx
  'Input voltage': 'Tensión de entrada',
  'Charge voltage': 'Tensión de carga',
  'Discharge voltage': 'Tensión de descarga',
  'Charge current': 'Corriente de carga',
  'Could not read battery status.': 'No se pudo leer el estado de la batería.',
  'Select a Wi-Fi panel to view its battery status.': 'Selecciona una central Wi-Fi para ver su estado de batería.',

  // FailuresScreen.tsx
  'Main power (220VAC) supply failure': 'Falla de alimentación principal (220VAC)',
  'Battery failure (too low or faulty)': 'Falla de batería (muy baja o defectuosa)',
  'Phone line failure': 'Falla de línea telefónica',
  'Siren 1 failure': 'Falla de sirena 1',
  'Siren 2 failure': 'Falla de sirena 2',
  '12V auxiliary power failure (keypads/accessories)': 'Falla de alimentación auxiliar de 12V (teclados/accesorios)',
  "Clock failure (lost the panel's time setting)": 'Falla de reloj (se perdió la hora configurada en la central)',
  'Cellular module failure': 'Falla del módulo celular',
  'Event communication failure': 'Falla de comunicación de eventos',
  'Keypad/accessory bus communication failure': 'Falla de comunicación del bus de teclados/accesorios',
  'Could not read failure status.': 'No se pudo leer el estado de fallas.',
  'Select a Wi-Fi panel to view its failures.': 'Selecciona una central Wi-Fi para ver sus fallas.',
  'No active faults.': 'No hay fallas activas.',

  // EventsScreen.tsx
  'Could not load events.': 'No se pudieron cargar los eventos.',
  'Select a Wi-Fi panel to view its events.': 'Selecciona una central Wi-Fi para ver sus eventos.',
  'No events yet.': 'Todavía no hay eventos.',

  // CustomMessagesScreen.tsx
  'Enter a message number between {min} and {max}.': 'Ingresa un número de mensaje entre {min} y {max}.',
  'Select an SMS panel first.': 'Selecciona primero una central SMS.',
  'Enter the message text.': 'Ingresa el texto del mensaje.',
  'Message must be {max} characters or fewer.': 'El mensaje debe tener {max} caracteres o menos.',
  'Read or write a message slot on "{name}" by text message.':
    'Lee o escribe una ranura de mensaje en "{name}" por mensaje de texto.',
  'Select an SMS panel to manage its messages.': 'Selecciona una central SMS para gestionar sus mensajes.',
  'Message number ({min}-{max})': 'Número de mensaje ({min}-{max})',
  'Message text (up to {max} characters)': 'Texto del mensaje (hasta {max} caracteres)',
  'Write message': 'Escribir mensaje',
  'Read message': 'Leer mensaje',

  // ContactPhoneScreen.tsx
  'Enter an order number between {min} and {max}.': 'Ingresa un número de orden entre {min} y {max}.',
  'Enter a phone number of 10 to 16 digits, no spaces or symbols.':
    'Ingresa un número de teléfono de 10 a 16 dígitos, sin espacios ni símbolos.',
  'Contact phone numbers': 'Números de contacto',
  'Manage a contact number on "{name}" by text message.': 'Gestiona un número de contacto en "{name}" por mensaje de texto.',
  'Select an SMS panel to manage its contact numbers.': 'Selecciona una central SMS para gestionar sus números de contacto.',
  'Order number ({min}-{max})': 'Número de orden ({min}-{max})',
  'Phone number (10-16 digits)': 'Número de teléfono (10-16 dígitos)',
  'Save number': 'Guardar número',
  'Read number': 'Leer número',
  'Delete number': 'Eliminar número',
  'Write phone number': 'Ingresar número de teléfono',
  'Read phone number': 'Leer número de teléfono',
  'Delete phone number': 'Borrar número de teléfono',

  // ClockScreen.tsx
  'Could not read the panel time.': 'No se pudo leer la hora de la central.',
  'Could not sync the panel time.': 'No se pudo sincronizar la hora de la central.',
  'Select a Wi-Fi panel to sync its clock.': 'Selecciona una central Wi-Fi para sincronizar su reloj.',
  'Panel time': 'Horario de el equipo',
  'Phone time': 'Horario actual',
  'Server time': 'Hora del servidor',
  'Sync panel to server time': 'Sincronizar Horario',

  // InstallerModeScreen.tsx
  'Your account is not authorized for Installer mode.': 'Tu cuenta no está autorizada para el modo instalador.',
  '(no response)': '(sin respuesta)',
  'Select a Wi-Fi panel to use Installer mode.': 'Selecciona una central Wi-Fi para usar el modo instalador.',
  Command: 'Comando',
  Send: 'Enviar',

  // ManagePanelScreen.tsx
  'This panel is no longer linked to your account.': 'Esta central ya no está vinculada a tu cuenta.',
  "This removes the panel from your account only -- it keeps working normally for anyone else who has it linked, and they won't need to log in again. You'll be logged out afterward.":
    'Esto quita la central solo de tu cuenta; sigue funcionando normalmente para cualquier otra persona que la tenga vinculada, y no necesitarán volver a iniciar sesión. Se cerrará tu sesión después.',
  "This permanently removes this SMS panel from your account. You'll be logged out afterward.":
    'Esto elimina permanentemente esta central SMS de tu cuenta. Se cerrará tu sesión después.',
  'Unlink this panel?': '¿Desvincular esta central?',
  'Unlink this panel': 'Desvincular esta central',
  Unlink: 'Desvincular',
  'Could not unlink this panel.': 'No se pudo desvincular esta central.',
  'Lock this panel?': '¿Bloquear esta central?',
  'Reset this panel?': '¿Restablecer esta central?',
  'This immediately revokes app control of the panel and signs out everyone who has it linked, including you. To restore access, link it again with the same identifier and user code.':
    'Esto revoca de inmediato el control de la app sobre la central y cierra la sesión de todos los que la tienen vinculada, incluido tú. Para restaurar el acceso, vinculala de nuevo con el mismo identificador y código de usuario.',
  "This wipes the panel from your account entirely and signs out everyone who has it linked, including you. You'll need to pair it again from scratch.":
    'Esto elimina la central de tu cuenta por completo y cierra la sesión de todos los que la tienen vinculada, incluido tú. Tendrás que emparejarla de nuevo desde cero.',
  Lock: 'Bloquear',
  Reset: 'Restablecer',
  'Could not complete this action.': 'No se pudo completar esta acción.',
  'Access code lockout': 'Bloqueo por código de usuario',
  'These affect everyone who has this panel linked, not just you.':
    'Estas acciones afectan a todos los que tienen esta central vinculada, no solo a ti.',
  'Lock this panel': 'Bloquear esta central',
  'Reset this panel': 'Restablecer esta central',
  Description: 'Descripción',
  Identifier: 'Identificador',
  'User code': 'Código de usuario',
  Update: 'Actualizar',

  // PanelSettingsScreen.tsx
  'Select a panel first.': 'Selecciona primero una central.',
  'Zones & outputs': 'Zonas y salidas',
  Diagnostics: 'Diagnóstico',
  Users: 'Usuarios',
  Advanced: 'Avanzado',
  'This panel': 'Esta central',
  Manage: 'Gestionar',
  'Unlink or lock this panel': 'Desvincular o bloquear esta central',

  // PanelSelectorScreen.tsx
  Search: 'Buscar',
  'No panels match your search.': 'Ninguna central coincide con tu búsqueda.',
  Online: 'En línea',
  Offline: 'Sin conexión',
  Selected: 'Seleccionada',

  // PanelIdentificationScreen.tsx
  'Enter a name for your panel.': 'Ingresa un nombre para tu central.',
  'Identify this panel': 'Identificar esta central',
  'Send a name to "{name}" by text message.': 'Envía un nombre a "{name}" por mensaje de texto.',
  'Select an SMS panel to give it a name.': 'Selecciona una central SMS para darle un nombre.',
  'Name to identify your alarm': 'Nombre para identificar tu alarma',

  // ScheduledDeparturesScreen.tsx
  Sun: 'Dom',
  Mon: 'Lun',
  Tue: 'Mar',
  Wed: 'Mié',
  Thu: 'Jue',
  Fri: 'Vie',
  Sat: 'Sáb',
  'New schedule': 'Nueva programación',
  Time: 'Hora',
  'Repeat on': 'Repetir en',
  Action: 'Acción',
  'Turn on': 'Activar',
  'Turn off': 'Desactivar',
  'Add schedule': 'Agregar programación',
  'Scheduled actions': 'Acciones programadas',
  'No schedules yet.': 'Todavía no hay programaciones.',
  'Choose at least one day.': 'Elige al menos un día.',
  'Could not save the schedule.': 'No se pudo guardar la programación.',
  'Could not update the schedule.': 'No se pudo actualizar la programación.',
  'Could not delete the schedule.': 'No se pudo eliminar la programación.',
  'Select a Wi-Fi panel to manage its schedules.': 'Selecciona una central Wi-Fi para administrar sus programaciones.',
  On: 'Encendido',
  Off: 'Apagado',
  Delete: 'Eliminar',
};
