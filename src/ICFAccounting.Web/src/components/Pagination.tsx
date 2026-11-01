interface PaginationProps {
  page: number
  totalPages: number
  totalCount: number
  disabled?: boolean
  onPageChange: (page: number) => void
}

export function Pagination({ page, totalPages, totalCount, disabled = false, onPageChange }: PaginationProps) {
  const lastPage = Math.max(totalPages, 1)
  return (
    <nav className="pagination" aria-label="Pagination">
      <button
        type="button"
        className="button button--secondary"
        onClick={() => onPageChange(page - 1)}
        disabled={disabled || page <= 1}
      >
        Previous
      </button>
      <span aria-live="polite">
        Page {page} of {lastPage} · {totalCount} {totalCount === 1 ? 'record' : 'records'}
      </span>
      <button
        type="button"
        className="button button--secondary"
        onClick={() => onPageChange(page + 1)}
        disabled={disabled || page >= lastPage}
      >
        Next
      </button>
    </nav>
  )
}
