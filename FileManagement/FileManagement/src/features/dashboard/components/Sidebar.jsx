import { Icon } from '../../../shared/components/Icon.jsx'
import { ThemedIcon } from '../../../shared/components/ThemedIcon.jsx'

const navTones = {
  files: 'blue',
  transactions: 'cyan',
  users: 'violet',
}

export function Sidebar({ navigationItems, activeView, onViewChange, user, onLogout }) {
  return (
    <aside className="sidebar surface-card">
      <div className="sidebar-brand">
        <span className="brand-mark">FM</span>
        <div>
          <span className="eyebrow">Workspace</span>
          <h2>File Management</h2>
        </div>
      </div>

      <nav className="nav-list">
        {navigationItems.map((item) => (
          <button
            key={item.key}
            type="button"
            className={`nav-item ${activeView === item.key ? 'active' : ''}`}
            onClick={() => onViewChange(item.key)}
          >
            <ThemedIcon
              name={item.icon}
              tone={activeView === item.key ? 'light' : navTones[item.key] ?? 'blue'}
              size="sm"
              className="nav-theme-icon"
            />
            <span className="nav-copy compact">
              <strong>{item.label}</strong>
            </span>
          </button>
        ))}
      </nav>

      <div className="sidebar-footer">
        <div className="user-chip">
          <Icon name="user" />
          <span>
            {user.displayName}
            {' · '}
            {user.roleLabel}
          </span>
        </div>
        <button type="button" className="secondary-button" onClick={onLogout}>
          <Icon name="logout" />
          <span>Logout</span>
        </button>
        <p className="sidebar-footer-note">Signed in as {user.email}</p>
      </div>
    </aside>
  )
}
