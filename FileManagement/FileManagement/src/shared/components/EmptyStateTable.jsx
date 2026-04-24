import { ThemedIcon } from './ThemedIcon.jsx'

export function EmptyStateTable({ columns, title, body, icon }) {
  return (
    <div>
      <div className="table-head">
        {columns.map((column) => (
          <span key={column}>{column}</span>
        ))}
      </div>

      <div className="empty-state">
        <ThemedIcon name={icon} tone="slate" size="lg" />
        <strong>{title}</strong>
        <p>{body}</p>
      </div>
    </div>
  )
}
