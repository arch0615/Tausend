import { createNavigationContainerRef } from '@react-navigation/native';

// Lets code outside the component tree (notifications/messaging.ts's tap handlers, which fire
// from native callbacks, not from a screen) trigger navigation once the container is mounted.
export const navigationRef = createNavigationContainerRef();
