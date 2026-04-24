import { useEffect, useState } from 'react'
import './App.css'
import { LoginPage } from '../features/auth/pages/LoginPage.jsx'
import { useAuthViewModel } from '../features/auth/hooks/useAuthViewModel.js'
import { DashboardLayout } from '../features/dashboard/components/DashboardLayout.jsx'
import { navigationItems } from '../features/dashboard/constants/navigation.js'
import { FilesPage } from '../features/dashboard/pages/FilesPage.jsx'
import { TransactionsPage } from '../features/dashboard/pages/TransactionsPage.jsx'
import { UsersPage } from '../features/dashboard/pages/UsersPage.jsx'

const viewTitles = {
  files: {
    title: 'File Imports',
    caption: '',
  },
  transactions: {
    title: 'Transactions',
    caption: '',
  },
  users: {
    title: 'User Management',
    caption: '',
  },
}

function App() {
  const auth = useAuthViewModel()
  const [activeView, setActiveView] = useState('files')
  const [transactionQuery, setTransactionQuery] = useState('')
  const [transactionType, setTransactionType] = useState('All')
  const [userRole, setUserRole] = useState('All')
  const workspaceNotice = ''

  const currentView = viewTitles[activeView] ?? viewTitles.files

  const roles = auth.session?.roles ?? []
  const permissions = auth.session?.permissions ?? []
  const isAdmin = roles.includes('Admin')
  const canViewUsers = isAdmin || permissions.includes('users.view')
  const canCreateUser = isAdmin || permissions.includes('users.create')
  const canUpdateUser = isAdmin || permissions.includes('users.update')
  const canDeleteUser = isAdmin || permissions.includes('users.delete')

  const visibleNavigationItems = navigationItems.filter((item) => {
    if (!item.requiredRoles && !item.requiredPermissions) {
      return true
    }

    const roleMatched =
      !item.requiredRoles || item.requiredRoles.some((role) => roles.includes(role))

    const permissionMatched =
      !item.requiredPermissions || item.requiredPermissions.some((permission) => permissions.includes(permission))

    return roleMatched || permissionMatched
  })

  useEffect(() => {
    if (activeView === 'users' && !canViewUsers) {
      setActiveView('files')
    }
  }, [activeView, canViewUsers])

  const renderView = () => {
    switch (activeView) {
      case 'files':
        return (
          <FilesPage
            accessToken={auth.session.accessToken}
            currentUserId={auth.session.id}
          />
        )
      case 'transactions':
        return (
          <TransactionsPage
            accessToken={auth.session.accessToken}
            query={transactionQuery}
            onQueryChange={setTransactionQuery}
            transactionType={transactionType}
            onTransactionTypeChange={setTransactionType}
          />
        )
      case 'users':
        if (!canViewUsers) {
          return (
            <FilesPage
              accessToken={auth.session.accessToken}
              currentUserId={auth.session.id}
            />
          )
        }

        return (
          <UsersPage
            userRole={userRole}
            onUserRoleChange={setUserRole}
            canCreateUser={canCreateUser}
            canUpdateUser={canUpdateUser}
            canDeleteUser={canDeleteUser}
            currentUserId={auth.session.id}
            accessToken={auth.session.accessToken}
          />
        )

      default:
        return (
          <FilesPage
            accessToken={auth.session.accessToken}
            currentUserId={auth.session.id}
          />
        )
    }
  }

  if (!auth.session) {
    return (
      <LoginPage
        email={auth.email}
        password={auth.password}
        fieldErrors={auth.fieldErrors}
        statusMessage={auth.statusMessage}
        statusTone={auth.statusTone}
        isSubmitting={auth.isSubmitting}
        onEmailChange={auth.onEmailChange}
        onPasswordChange={auth.onPasswordChange}
        onEmailBlur={auth.onEmailBlur}
        onPasswordBlur={auth.onPasswordBlur}
        onSubmit={auth.login}
      />
    )
  }

  return (
    <DashboardLayout
      navigationItems={visibleNavigationItems}
      activeView={activeView}
      onViewChange={setActiveView}
      user={auth.session}
      viewTitle={currentView.title}
      viewCaption={currentView.caption}
      workspaceNotice={workspaceNotice}
      onLogout={auth.logout}
    >
      {renderView()}
    </DashboardLayout>
  )
}

export default App
