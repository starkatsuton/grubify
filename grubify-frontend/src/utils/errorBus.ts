// Lightweight publish/subscribe bus used to notify the app (from outside
// React component tree, e.g. axios interceptors) that a server error occurred.
export interface ServerErrorDetail {
  message: string;
  status?: number;
}

type Listener = (detail: ServerErrorDetail) => void;

const listeners = new Set<Listener>();

export const emitServerError = (detail: ServerErrorDetail): void => {
  listeners.forEach(listener => listener(detail));
};

export const subscribeServerError = (listener: Listener): (() => void) => {
  listeners.add(listener);
  return () => listeners.delete(listener);
};
