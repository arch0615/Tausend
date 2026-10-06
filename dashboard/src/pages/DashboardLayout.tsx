import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import './DashboardLayout.css'

export function DashboardLayout() {
  const { session, logout } = useAuth()

  return (
    <div className="layout">
      <aside className="sidebar">
        <div className="brand">Tausend Admin</div>
        <nav>
          <NavLink to="/" end className={({ isActive }) => (isActive ? 'nav-link active' : 'nav-link')}>
            Overview
          </NavLink>
          <NavLink to="/accounts" className={({ isActive }) => (isActive ? 'nav-link active' : 'nav-link')}>
            Accounts
          </NavLink>
          <NavLink to="/devices" className={({ isActive }) => (isActive ? 'nav-link active' : 'nav-link')}>
            Devices
          </NavLink>
          <NavLink to="/events" className={({ isActive }) => (isActive ? 'nav-link active' : 'nav-link')}>
            Events
          </NavLink>
          <NavLink to="/audit-log" className={({ isActive }) => (isActive ? 'nav-link active' : 'nav-link')}>
            Audit log
          </NavLink>
          <NavLink to="/system-status" className={({ isActive }) => (isActive ? 'nav-link active' : 'nav-link')}>
            System status
          </NavLink>
          <NavLink to="/settings" className={({ isActive }) => (isActive ? 'nav-link active' : 'nav-link')}>
            Settings
          </NavLink>
        </nav>
      </aside>
      <div className="main">
        <header className="topbar">
          <span className="signed-in-as">{session?.account.Email}</span>
          <button className="logout-btn" onClick={logout}>
            Log out
          </button>
        </header>
        <main className="content">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
