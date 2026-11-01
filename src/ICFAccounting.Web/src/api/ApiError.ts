export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  /** Present on ValidationProblemDetails responses. */
  errors?: Record<string, string[]>
}

export type ApiErrorKind = 'network' | 'http'

/** Error raised by the API client for unreachable servers and non-success responses. */
export class ApiError extends Error {
  readonly kind: ApiErrorKind
  readonly status: number | undefined
  readonly problem: ProblemDetails | undefined

  constructor(kind: ApiErrorKind, message: string, status?: number, problem?: ProblemDetails) {
    super(message)
    this.name = 'ApiError'
    this.kind = kind
    this.status = status
    this.problem = problem
  }

  get isConflict(): boolean {
    return this.status === 409
  }

  get isNotFound(): boolean {
    return this.status === 404
  }
}

export function isAbortError(error: unknown): boolean {
  return error instanceof DOMException && error.name === 'AbortError'
}
