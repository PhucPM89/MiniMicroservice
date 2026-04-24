export const navigationItems = [
  {
    key: 'files',
    label: 'Files',
    icon: 'files',
  },
  {
    key: 'transactions',
    label: 'Transactions',
    icon: 'transactions',
  },
  {
    key: 'users',
    label: 'Users',
    icon: 'users',
    requiredRoles: ['Admin'],
    requiredPermissions: ['users.view']
  },
]
