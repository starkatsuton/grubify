import { useEffect } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { subscribeServerError } from '../utils/errorBus';

/**
 * Mounted once near the root of the app (inside the Router). Listens for
 * server error notifications emitted by the API layer and redirects the
 * user to the dedicated error page instead of letting failures happen
 * silently.
 */
const GlobalErrorHandler: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();

  useEffect(() => {
    const unsubscribe = subscribeServerError(detail => {
      if (location.pathname === '/error') {
        return;
      }
      navigate('/error', { state: detail });
    });
    return unsubscribe;
  }, [navigate, location.pathname]);

  return null;
};

export default GlobalErrorHandler;
