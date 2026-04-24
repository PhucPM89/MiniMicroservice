import { Icon } from '../../../shared/components/Icon.jsx'
import { ThemedIcon } from '../../../shared/components/ThemedIcon.jsx'

export function AppHeader({ title, caption, workspaceNotice }) {
  return (
    <>
      <header className="workspace-header surface-card">
        <div className="header-copy">
          <h1>{title}</h1>
          {caption ? <p>{caption}</p> : null}
        </div>

        <span className="feature-chip compact">
          <ThemedIcon name="spark" tone="blue" size="sm" />
        </span>
      </header>

      {workspaceNotice ? (
        <div className="workspace-note subtle">
          <Icon name="info" />
          <span>{workspaceNotice}</span>
        </div>
      ) : null}
    </>
  )
}
