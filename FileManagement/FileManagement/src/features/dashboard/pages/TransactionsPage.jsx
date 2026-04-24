import { useEffect, useState } from 'react'
import { Icon } from '../../../shared/components/Icon.jsx'
import { EmptyStateTable } from '../../../shared/components/EmptyStateTable.jsx'
import { ThemedIcon } from '../../../shared/components/ThemedIcon.jsx'
import { getTransactions } from '../services/transactionService.js'

const amountFormatter = new Intl.NumberFormat('en-US', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
})

export function TransactionsPage({
  accessToken,
  query,
  onQueryChange,
  transactionType,
  onTransactionTypeChange,
}) {
  const [transactions, setTransactions] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState('')
  const [currentCursor, setCurrentCursor] = useState(null)
  const [cursorHistory, setCursorHistory] = useState([])
  const [nextCursor, setNextCursor] = useState(null)
  const [hasMore, setHasMore] = useState(false)

  useEffect(() => {
    setTransactions([])
    setErrorMessage('')
    setCurrentCursor(null)
    setCursorHistory([])
    setNextCursor(null)
    setHasMore(false)
  }, [accessToken, query, transactionType])

  useEffect(() => {
    let isMounted = true

    const loadTransactions = async () => {
      try {
        setIsLoading(true)
        setErrorMessage('')

        const page = await getTransactions(accessToken, {
          transactionId: query,
          type: transactionType,
          cursor: currentCursor,
        })

        if (isMounted) {
          setTransactions(page.items)
          setNextCursor(page.nextCursor)
          setHasMore(page.hasMore)
        }
      } catch (error) {
        if (isMounted) {
          setErrorMessage(error.message ?? 'Failed to load transactions.')
        }
      } finally {
        if (isMounted) {
          setIsLoading(false)
        }
      }
    }

    loadTransactions()

    return () => {
      isMounted = false
    }
  }, [accessToken, query, transactionType, currentCursor])

  const currentPage = cursorHistory.length + 1
  const canGoPrevious = cursorHistory.length > 0
  const canGoNext = hasMore && Boolean(nextCursor)

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

  return (
    <div className="view-stack">
      <article className="surface-card page-section-card">
        <div className="section-card-header">
          <div className="section-card-title">
            <ThemedIcon name="transactions" tone="cyan" size="md" />
            <div>
              <h2>Filter transactions</h2>
              <p>Search and narrow the transaction stream.</p>
            </div>
          </div>
        </div>

        <div className="section-card-body filter-row">
          <label className="field">
            <span className="field-label">Search</span>
            <div className="input-shell">
              <Icon name="search" />
              <input
                type="text"
                placeholder="Search by transaction id"
                value={query}
                onChange={(event) => onQueryChange(event.target.value)}
              />
            </div>
          </label>

          <label className="field">
            <span className="field-label">Type</span>
            <div className="input-shell">
              <Icon name="filter" />
              <select value={transactionType} onChange={(event) => onTransactionTypeChange(event.target.value)}>
                <option value="All">All</option>
                <option value="Credit">Credit</option>
                <option value="Debit">Debit</option>
                <option value="Refund">Refund</option>
              </select>
            </div>
          </label>
        </div>
      </article>

      <article className="surface-card page-section-card">
        <div className="section-card-header">
          <div className="section-card-title">
            <ThemedIcon name="transactions" tone="slate" size="md" />
            <div>
              <h2>Results table</h2>
              <p>Transactions fetched from TransactionService.</p>
            </div>
          </div>
          <span className="status-badge success">
            <span className="status-dot" />
            <span>Live API</span>
          </span>
        </div>

        {isLoading ? (
          <div className="table-placeholder">Loading transactions...</div>
        ) : errorMessage ? (
          <div className="table-placeholder error">{errorMessage}</div>
        ) : transactions.length === 0 ? (
          <EmptyStateTable
            columns={['Transaction id', 'File', 'Type', 'Amount', 'Created at']}
            title="No transactions yet"
            body="No transaction matched the selected filter."
            icon="transactions"
          />
        ) : (
          <div className="transactions-table-wrapper">
            <div className="table-head transactions-table">
              <span>Transaction id</span>
              <span>File</span>
              <span>Type</span>
              <span>Amount</span>
              <span>Created at</span>
            </div>

            {transactions.map((transaction) => (
              <div key={transaction.id} className="table-row transactions-table">
                <span className="table-primary-text">{transaction.transactionId}</span>
                <span>{transaction.fileName}</span>
                <span>{transaction.type}</span>
                <span>{amountFormatter.format(transaction.amount)}</span>
                <span>{new Date(transaction.createdAtUtc).toLocaleString()}</span>
              </div>
            ))}
          </div>
        )}

        {transactions.length > 0 || canGoPrevious || canGoNext ? (
          <div className="table-footer-actions">
            <div className="table-pagination-status">
              <span>Page {currentPage}</span>
              <span>{hasMore ? 'More transactions available' : 'End of results'}</span>
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
      </article>
    </div>
  )
}
