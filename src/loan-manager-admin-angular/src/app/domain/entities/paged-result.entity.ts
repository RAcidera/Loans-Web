// Domain layer — wire shape for server-side-paged list endpoints.

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
}
