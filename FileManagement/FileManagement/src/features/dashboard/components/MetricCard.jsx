import { Icon } from '../../../shared/components/Icon.jsx'

export function MetricCard({ eyebrow, title, description, icon }) {
  return (
    <article className="surface-card metric-card">
      <div className="metric-top">
        <span className="eyebrow">{eyebrow}</span>
        <span className="icon-badge light">
          <Icon name={icon} />
        </span>
      </div>
      <strong>{title}</strong>
      <p>{description}</p>
    </article>
  )
}
