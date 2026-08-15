import { HttpErrorResponse } from '@angular/common/http';

interface ApiValidationError {
  propertyName?: string;
  errorMessage?: string;
}

interface ApiErrorBody {
  message?: string;
  code?: string;
  errors?: ApiValidationError[];
}

/**
 * Reads the API error JSON (`message`, FluentValidation `errors`, or `code: concurrency`).
 * Prefer this over raw `HttpErrorResponse.error` in feature pages.
 */
export function apiErrorMessage(
  err: HttpErrorResponse,
  fallback = 'Something went wrong. Please try again.'
): string {
  const body = err.error as ApiErrorBody | string | null | undefined;

  if (typeof body === 'string' && body.trim()) {
    return body;
  }

  if (body && typeof body === 'object') {
    const validationMessages = body.errors
      ?.map((e) => e.errorMessage?.trim())
      .filter((msg): msg is string => !!msg);

    if (validationMessages && validationMessages.length > 0) {
      return validationMessages.join(' ');
    }

    if (body.code === 'concurrency') {
      return body.message ?? 'This product was updated by someone else. Refresh and try again.';
    }

    if (body.message?.trim()) {
      return body.message;
    }
  }

  return fallback;
}
