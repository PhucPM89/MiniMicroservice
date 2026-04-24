import { Sidebar } from './Sidebar.jsx'
import { AppHeader } from './AppHeader.jsx'

export function DashboardLayout({
  children,
  navigationItems,
  activeView,
  onViewChange,
  user,
  viewTitle,
  viewCaption,
  workspaceNotice,
  onLogout,
}) {
  return (
    <div className="dashboard-shell">
      <Sidebar
        navigationItems={navigationItems}
        activeView={activeView}
        onViewChange={onViewChange}
        user={user}
        onLogout={onLogout}
      />

      <main className="workspace">
        <AppHeader title={viewTitle} caption={viewCaption} workspaceNotice={workspaceNotice} />
        <section className="workspace-body">{children}</section>
      </main>
    </div>
  )
}
