import { Icon } from './Icon.jsx'

export function ThemedIcon({ name, tone = 'blue', size = 'md', className = '' }) {
  const classes = ['theme-icon', `theme-icon--${tone}`, `theme-icon--${size}`, className]
    .filter(Boolean)
    .join(' ')

  return (
    <span className={classes} aria-hidden="true">
      <Icon name={name} />
    </span>
  )
}
