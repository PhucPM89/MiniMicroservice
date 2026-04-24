import { useEffect, useState } from 'react'
import { Icon } from '../../../shared/components/Icon.jsx'
import { EmptyStateTable } from '../../../shared/components/EmptyStateTable.jsx'
import { ThemedIcon } from '../../../shared/components/ThemedIcon.jsx'
import { createUser, deleteUser, getUsers, updateUser } from '../services/userService.js'

function resolvePrimaryRole(user) {
  if (user.roles.includes('Admin')) {
    return 'Admin'
  }

  return 'User'
}

export function UsersPage({
  userRole,
  onUserRoleChange,
  canCreateUser,
  canUpdateUser,
  canDeleteUser,
  currentUserId,
  accessToken,
}) {
  const showActionColumn = canUpdateUser || canDeleteUser
  const tableColumns = ['Email', 'Role', 'Status', 'Created at', 'Permissions']
  const [users, setUsers] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState('')
  const [currentCursor, setCurrentCursor] = useState(null)
  const [cursorHistory, setCursorHistory] = useState([])
  const [nextCursor, setNextCursor] = useState(null)
  const [hasMore, setHasMore] = useState(false)
  const [editingUser, setEditingUser] = useState(null)
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false)
  const [createForm, setCreateForm] = useState({
    email: '',
    password: '',
    role: 'User',
  })
  const [editForm, setEditForm] = useState({
    email: '',
    password: '',
    role: 'User',
  })
  const [actionErrorMessage, setActionErrorMessage] = useState('')
  const [actionUserId, setActionUserId] = useState(null)
  const [isSubmittingCreate, setIsSubmittingCreate] = useState(false)
  const [isSubmittingEdit, setIsSubmittingEdit] = useState(false)

  useEffect(() => {
    setUsers([])
    setErrorMessage('')
    setCurrentCursor(null)
    setCursorHistory([])
    setNextCursor(null)
    setHasMore(false)
    setIsCreateModalOpen(false)
    setEditingUser(null)
    setActionErrorMessage('')
    setActionUserId(null)
  }, [accessToken, userRole])

  useEffect(() => {
    let isMounted = true

    const loadUsers = async () => {
      try {
        setIsLoading(true)
        setErrorMessage('')

        const page = await getUsers(accessToken, {
          role: userRole,
          cursor: currentCursor,
        })

        if (isMounted) {
          setUsers(page.items)
          setNextCursor(page.nextCursor)
          setHasMore(page.hasMore)
        }
      } catch (error) {
        if (isMounted) {
          setErrorMessage(error.message ?? 'Failed to load users.')
        }
      } finally {
        if (isMounted) {
          setIsLoading(false)
        }
      }
    }

    loadUsers()

    return () => {
      isMounted = false
    }
  }, [accessToken, userRole, currentCursor])

  if (showActionColumn) {
    tableColumns.push('Action')
  }

  const currentPage = cursorHistory.length + 1
  const canGoPrevious = cursorHistory.length > 0
  const canGoNext = hasMore && Boolean(nextCursor)

  const refreshCurrentPage = async () => {
    const page = await getUsers(accessToken, {
      role: userRole,
      cursor: currentCursor,
    })

    setUsers(page.items)
    setNextCursor(page.nextCursor)
    setHasMore(page.hasMore)
  }

  const resetToFirstPage = async () => {
    setCursorHistory([])

    if (currentCursor) {
      setCurrentCursor(null)
      return
    }

    await refreshCurrentPage()
  }

  const handleNextPage = () => {
    if (!nextCursor || isLoading) {
      return
    }

    setCursorHistory((current) => [...current, currentCursor])
    setCurrentCursor(nextCursor)
  }

  const handlePreviousPage = () => {
    if (cursorHistory.length === 0 || isLoading) {
      return
    }

    const previousCursor = cursorHistory[cursorHistory.length - 1]
    setCursorHistory((current) => current.slice(0, -1))
    setCurrentCursor(previousCursor)
  }

  const handleEditClick = (user) => {
    setActionErrorMessage('')
    setEditingUser(user)
    setEditForm({
      email: user.email,
      password: '',
      role: resolvePrimaryRole(user),
    })
  }

  const handleCreateClick = () => {
    setActionErrorMessage('')
    setIsCreateModalOpen(true)
    setCreateForm({
      email: '',
      password: '',
      role: 'User',
    })
  }

  const closeCreateModal = () => {
    setIsCreateModalOpen(false)
    setActionErrorMessage('')
    setCreateForm({
      email: '',
      password: '',
      role: 'User',
    })
  }

  const closeEditModal = () => {
    setEditingUser(null)
    setActionErrorMessage('')
    setEditForm({
      email: '',
      password: '',
      role: 'User',
    })
  }

  const handleCreateSubmit = async (event) => {
    event.preventDefault()

    try {
      setIsSubmittingCreate(true)
      setActionErrorMessage('')

      await createUser(accessToken, {
        email: createForm.email.trim(),
        password: createForm.password.trim(),
        isActive: true,
        roleNames: [createForm.role],
      })

      closeCreateModal()
      await resetToFirstPage()
    } catch (error) {
      setActionErrorMessage(error.message ?? 'Failed to create user.')
    } finally {
      setIsSubmittingCreate(false)
    }
  }

  const handleEditSubmit = async (event) => {
    event.preventDefault()

    if (!editingUser) {
      return
    }

    try {
      setIsSubmittingEdit(true)
      setActionErrorMessage('')

      const payload = {
        email: editForm.email.trim(),
        roleNames: [editForm.role],
      }

      if (editForm.password.trim()) {
        payload.password = editForm.password.trim()
      }

      await updateUser(accessToken, editingUser.id, payload)
      closeEditModal()
      await refreshCurrentPage()
    } catch (error) {
      setActionErrorMessage(error.message ?? 'Failed to update user.')
    } finally {
      setIsSubmittingEdit(false)
    }
  }

  const isEditingCurrentUser = editingUser?.id === currentUserId

  const handleStatusToggle = async (user) => {
    if (user.id === currentUserId) {
      return
    }

    try {
      setActionUserId(user.id)
      setActionErrorMessage('')

      if (user.isActive) {
        await deleteUser(accessToken, user.id)
      } else {
        await updateUser(accessToken, user.id, { isActive: true })
      }

      await refreshCurrentPage()
    } catch (error) {
      setActionErrorMessage(error.message ?? 'Failed to update user status.')
    } finally {
      setActionUserId(null)
    }
  }

  return (
    <div className="view-stack">
      <article className="surface-card page-section-card">
        <div className="section-card-header">
          <div className="section-card-title">
            <ThemedIcon name="users" tone="violet" size="md" />
            <div>
              <h2>Filter users</h2>
              <p>Limit the table by access role.</p>
            </div>
          </div>
        </div>

        {canCreateUser ? (
          <div className="user-action-row">
            <button
              type="button"
              className="primary-button compact-action"
              onClick={handleCreateClick}
              disabled={isSubmittingCreate || isSubmittingEdit}
            >
              Create user
            </button>
          </div>
        ) : null}

        <div className="section-card-body filter-row single">
          <label className="field">
            <span className="field-label">Role</span>
            <div className="input-shell">
              <Icon name="filter" />
              <select value={userRole} onChange={(event) => onUserRoleChange(event.target.value)}>
                <option value="All">All</option>
                <option value="Admin">Admin</option>
                <option value="User">User</option>
              </select>
            </div>
          </label>
        </div>
      </article>

      <article className="surface-card page-section-card">
        <div className="section-card-header">
          <div className="section-card-title">
            <ThemedIcon name="users" tone="slate" size="md" />
            <div>
              <h2>Management table</h2>
              <p>User records will render here.</p>
            </div>
          </div>
          <span className="status-badge success">
            <span className="status-dot" />
            <span>Live API</span>
          </span>
        </div>

        {actionErrorMessage ? (
          <div className="table-placeholder error table-action-feedback">{actionErrorMessage}</div>
        ) : null}

        {isLoading ? (
          <div className="table-placeholder">Loading users...</div>
        ) : errorMessage ? (
          <div className="table-placeholder error">{errorMessage}</div>
        ) : users.length === 0 ? (
          <EmptyStateTable
            columns={tableColumns}
            title="No users found"
            body="No user matched the selected filter."
            icon="users"
          />
        ) : (
          <div className="users-table">
            <div className={`table-head ${showActionColumn ? 'with-actions' : ''}`}>
              {tableColumns.map((column) => (
                <span key={column}>{column}</span>
              ))}
            </div>

            {users.map((user) => {
              const isProcessingStatus = actionUserId === user.id
              const canToggleStatus = user.isActive ? canDeleteUser : canUpdateUser
              const isCurrentUser = user.id === currentUserId

              return (
                <div key={user.id} className={`table-row ${showActionColumn ? 'with-actions' : ''}`}>
                  <span>{user.email}</span>
                  <span>{user.roles.join(', ') || 'No role'}</span>
                  <span>{user.isActive ? 'Active' : 'Inactive'}</span>
                  <span>{new Date(user.createdAtUtc).toLocaleString()}</span>
                  <span className="table-cell-permissions">
                    {user.permissions.join(', ') || 'No permissions'}
                  </span>
                  {showActionColumn ? (
                    <div className="row-action-cell">
                      <div className="row-action-buttons">
                        {canUpdateUser ? (
                          <button
                            type="button"
                            className="secondary-button table-inline-action"
                            onClick={() => handleEditClick(user)}
                            disabled={isProcessingStatus || isSubmittingEdit}
                          >
                            Edit
                          </button>
                        ) : null}
                        {canToggleStatus ? (
                          <button
                            type="button"
                            className={`status-toggle ${user.isActive ? 'is-active' : 'is-inactive'}`}
                            onClick={() => handleStatusToggle(user)}
                            disabled={isCurrentUser || isProcessingStatus || isSubmittingEdit}
                            aria-pressed={user.isActive}
                            title={isCurrentUser ? 'You cannot change your own account status.' : undefined}
                          >
                            <span className="status-toggle-shell">
                              <span className="status-toggle-text">
                                {isProcessingStatus ? '...' : user.isActive ? 'ON' : 'OFF'}
                              </span>
                              <span className="status-toggle-knob" />
                            </span>
                          </button>
                        ) : null}
                      </div>
                    </div>
                  ) : null}
                </div>
              )
            })}
          </div>
        )}

        {users.length > 0 || canGoPrevious || canGoNext ? (
          <div className="table-footer-actions">
            <div className="table-pagination-status">
              <span>Page {currentPage}</span>
              <span>{hasMore ? 'More records available' : 'End of results'}</span>
            </div>
            <div className="table-pagination-actions">
              <button
                type="button"
                className="secondary-button compact-action"
                onClick={handlePreviousPage}
                disabled={!canGoPrevious || isLoading}
              >
                Previous
              </button>
              <button
                type="button"
                className="secondary-button compact-action"
                onClick={handleNextPage}
                disabled={!canGoNext || isLoading}
              >
                Next
              </button>
            </div>
          </div>
        ) : null}

        {isLoading && users.length > 0 ? (
          <div className="table-footer-loading">
            Loading page...
          </div>
        ) : null}
      </article>

      {isCreateModalOpen ? (
        <div className="modal-backdrop">
          <div className="modal-card">
            <div className="modal-header">
              <div>
                <h3>Create user</h3>
                <p>Create a new workspace account with a primary role.</p>
              </div>
              <button type="button" className="ghost-button modal-close-button" onClick={closeCreateModal}>
                Close
              </button>
            </div>

            <form className="modal-form" onSubmit={handleCreateSubmit}>
              <label className="field">
                <span className="field-label">Email</span>
                <div className="input-shell">
                  <Icon name="mail" />
                  <input
                    type="email"
                    value={createForm.email}
                    onChange={(event) =>
                      setCreateForm((current) => ({ ...current, email: event.target.value }))
                    }
                    required
                    maxLength={255}
                  />
                </div>
              </label>

              <label className="field">
                <span className="field-label">Password</span>
                <div className="input-shell">
                  <Icon name="lock" />
                  <input
                    type="password"
                    value={createForm.password}
                    onChange={(event) =>
                      setCreateForm((current) => ({ ...current, password: event.target.value }))
                    }
                    placeholder="Enter a temporary password"
                    required
                    minLength={8}
                    maxLength={100}
                  />
                </div>
              </label>

              <label className="field">
                <span className="field-label">Role</span>
                <div className="input-shell">
                  <Icon name="filter" />
                  <select
                    value={createForm.role}
                    onChange={(event) =>
                      setCreateForm((current) => ({ ...current, role: event.target.value }))
                    }
                  >
                    <option value="Admin">Admin</option>
                    <option value="User">User</option>
                  </select>
                </div>
              </label>

              {actionErrorMessage ? (
                <div className="table-placeholder error">{actionErrorMessage}</div>
              ) : null}

              <div className="modal-actions">
                <button type="button" className="secondary-button" onClick={closeCreateModal}>
                  Cancel
                </button>
                <button type="submit" className="primary-button" disabled={isSubmittingCreate}>
                  {isSubmittingCreate ? 'Creating...' : 'Create user'}
                </button>
              </div>
            </form>
          </div>
        </div>
      ) : null}

      {editingUser ? (
        <div className="modal-backdrop">
          <div className="modal-card">
            <div className="modal-header">
              <div>
                <h3>Edit user</h3>
                <p>Update profile information and primary role.</p>
              </div>
              <button type="button" className="ghost-button modal-close-button" onClick={closeEditModal}>
                Close
              </button>
            </div>

            <form className="modal-form" onSubmit={handleEditSubmit}>
              <label className="field">
                <span className="field-label">Email</span>
                <div className="input-shell">
                  <Icon name="mail" />
                  <input
                    type="email"
                    value={editForm.email}
                    onChange={(event) =>
                      setEditForm((current) => ({ ...current, email: event.target.value }))
                    }
                    required
                    maxLength={255}
                  />
                </div>
              </label>

              <label className="field">
                <span className="field-label">Password</span>
                <div className="input-shell">
                  <Icon name="lock" />
                  <input
                    type="password"
                    value={editForm.password}
                    onChange={(event) =>
                      setEditForm((current) => ({ ...current, password: event.target.value }))
                    }
                    placeholder="Leave blank to keep the current password"
                    minLength={8}
                    maxLength={100}
                  />
                </div>
              </label>

              <label className="field">
                <span className="field-label">Role</span>
                <div className="input-shell">
                  <Icon name="filter" />
                  <select
                    value={editForm.role}
                    onChange={(event) =>
                      setEditForm((current) => ({ ...current, role: event.target.value }))
                    }
                    disabled={isEditingCurrentUser}
                  >
                    <option value="Admin">Admin</option>
                    <option value="User">User</option>
                  </select>
                </div>
              </label>

              {isEditingCurrentUser ? (
                <div className="modal-inline-note">
                  You cannot change your own role from this screen.
                </div>
              ) : null}

              {actionErrorMessage ? (
                <div className="table-placeholder error">{actionErrorMessage}</div>
              ) : null}

              <div className="modal-actions">
                <button type="button" className="secondary-button" onClick={closeEditModal}>
                  Cancel
                </button>
                <button type="submit" className="primary-button" disabled={isSubmittingEdit}>
                  {isSubmittingEdit ? 'Saving...' : 'Save changes'}
                </button>
              </div>
            </form>
          </div>
        </div>
      ) : null}
    </div>
  )
}
