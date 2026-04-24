export function Icon({ name }) {
  const common = {
    fill: 'none',
    stroke: 'currentColor',
    strokeWidth: '1.85',
    strokeLinecap: 'round',
    strokeLinejoin: 'round',
  }

  const icons = {
    overview: (
      <>
        <rect x="3" y="3" width="7" height="7" rx="1.6" {...common} />
        <rect x="14" y="3" width="7" height="7" rx="1.6" {...common} />
        <rect x="3" y="14" width="7" height="7" rx="1.6" {...common} />
        <rect x="14" y="14" width="7" height="7" rx="1.6" {...common} />
      </>
    ),
    files: (
      <>
        <path d="M4 7.5A2.5 2.5 0 0 1 6.5 5H10l2 2h5.5A2.5 2.5 0 0 1 20 9.5v8a2.5 2.5 0 0 1-2.5 2.5h-11A2.5 2.5 0 0 1 4 17.5z" {...common} />
      </>
    ),
    transactions: (
      <>
        <path d="M6 4.5h8.5l3.5 3.5v11.5A1.5 1.5 0 0 1 16.5 21h-9A1.5 1.5 0 0 1 6 19.5z" {...common} />
        <path d="M9 10.5h6M9 14.5h6M9 7h3" {...common} />
      </>
    ),
    users: (
      <>
        <path d="M9 11a3 3 0 1 0 0-6 3 3 0 0 0 0 6Z" {...common} />
        <path d="M15.5 9.5a2.5 2.5 0 1 0 0-5" {...common} />
        <path d="M4.5 19a4.5 4.5 0 0 1 9 0" {...common} />
        <path d="M14.5 18a3.5 3.5 0 0 1 5 0" {...common} />
      </>
    ),
    shield: (
      <>
        <path d="M12 3l7 3v5c0 4.5-3 7.5-7 10-4-2.5-7-5.5-7-10V6z" {...common} />
        <path d="m9.5 12 1.8 1.8 3.8-4" {...common} />
      </>
    ),
    upload: (
      <>
        <path d="M12 16V6" {...common} />
        <path d="m8 10 4-4 4 4" {...common} />
        <path d="M5 19h14" {...common} />
      </>
    ),
    search: (
      <>
        <circle cx="11" cy="11" r="6" {...common} />
        <path d="m20 20-4.2-4.2" {...common} />
      </>
    ),
    filter: (
      <>
        <path d="M4 6h16" {...common} />
        <path d="M7 12h10" {...common} />
        <path d="M10 18h4" {...common} />
      </>
    ),
    user: (
      <>
        <path d="M12 12a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7Z" {...common} />
        <path d="M5 20a7 7 0 0 1 14 0" {...common} />
      </>
    ),
    logout: (
      <>
        <path d="M10 5H6.5A1.5 1.5 0 0 0 5 6.5v11A1.5 1.5 0 0 0 6.5 19H10" {...common} />
        <path d="M14 16l4-4-4-4" {...common} />
        <path d="M18 12H9" {...common} />
      </>
    ),
    lock: (
      <>
        <rect x="5" y="10" width="14" height="10" rx="2" {...common} />
        <path d="M8 10V8a4 4 0 0 1 8 0v2" {...common} />
      </>
    ),
    mail: (
      <>
        <rect x="3.5" y="5.5" width="17" height="13" rx="2" {...common} />
        <path d="m5 8 7 5 7-5" {...common} />
      </>
    ),
    login: (
      <>
        <path d="M14 16l4-4-4-4" {...common} />
        <path d="M18 12H9" {...common} />
        <path d="M9 5H6.5A1.5 1.5 0 0 0 5 6.5v11A1.5 1.5 0 0 0 6.5 19H9" {...common} />
      </>
    ),
    info: (
      <>
        <circle cx="12" cy="12" r="9" {...common} />
        <path d="M12 10v5" {...common} />
        <path d="M12 7.5h.01" {...common} />
      </>
    ),
    warning: (
      <>
        <path d="M12 4.5 20 19H4z" {...common} />
        <path d="M12 9.5v4.5" {...common} />
        <path d="M12 16.8h.01" {...common} />
      </>
    ),
    spark: (
      <>
        <path d="m12 3 1.8 4.2L18 9l-4.2 1.8L12 15l-1.8-4.2L6 9l4.2-1.8z" {...common} />
        <path d="m18.5 15 .8 1.8 1.7.7-1.7.7-.8 1.8-.8-1.8-1.7-.7 1.7-.7zM5.5 14l.7 1.5 1.5.7-1.5.7-.7 1.5-.7-1.5-1.5-.7 1.5-.7z" {...common} />
      </>
    ),
    server: (
      <>
        <rect x="4" y="4" width="16" height="6" rx="2" {...common} />
        <rect x="4" y="14" width="16" height="6" rx="2" {...common} />
        <path d="M8 7h.01M8 17h.01M12 7h4M12 17h4" {...common} />
      </>
    ),
    clock: (
      <>
        <circle cx="12" cy="12" r="8.5" {...common} />
        <path d="M12 8v4.5l3 2" {...common} />
      </>
    ),
  }

  return (
    <svg className="icon" viewBox="0 0 24 24" aria-hidden="true">
      {icons[name] ?? icons.info}
    </svg>
  )
}
