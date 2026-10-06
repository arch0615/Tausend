import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { AuthProvider } from './auth/AuthContext'
import { RequireAdmin } from './auth/RequireAdmin'
import { LoginPage } from './pages/LoginPage'
import { ResetPasswordPage } from './pages/ResetPasswordPage'
import { DashboardLayout } from './pages/DashboardLayout'
import { OverviewPage } from './pages/OverviewPage'
import { AccountsPage } from './pages/AccountsPage'
import { AccountDetailPage } from './pages/AccountDetailPage'
import { DevicesPage } from './pages/DevicesPage'
import { DeviceDetailPage } from './pages/DeviceDetailPage'
import { DeviceUsersPage } from './pages/DeviceUsersPage'
import { EventsPage } from './pages/EventsPage'
import { AuditLogPage } from './pages/AuditLogPage'
import { SettingsPage } from './pages/SettingsPage'
import { SystemStatusPage } from './pages/SystemStatusPage'

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/reset-password" element={<ResetPasswordPage />} />
          <Route element={<RequireAdmin />}>
            <Route element={<DashboardLayout />}>
              <Route path="/" element={<OverviewPage />} />
              <Route path="/accounts" element={<AccountsPage />} />
              <Route path="/accounts/:accountId" element={<AccountDetailPage />} />
              <Route path="/devices" element={<DevicesPage />} />
              <Route path="/devices/:deviceId" element={<DeviceDetailPage />} />
              <Route path="/devices/:deviceId/users" element={<DeviceUsersPage />} />
              <Route path="/events" element={<EventsPage />} />
              <Route path="/audit-log" element={<AuditLogPage />} />
              <Route path="/system-status" element={<SystemStatusPage />} />
              <Route path="/settings" element={<SettingsPage />} />
            </Route>
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}
