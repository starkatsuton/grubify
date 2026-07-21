import React from 'react';
import { Box, Container, Typography, Button, Paper } from '@mui/material';
import { ErrorOutline as ErrorIcon } from '@mui/icons-material';
import { useLocation, useNavigate } from 'react-router-dom';

interface ErrorPageState {
  message?: string;
  status?: number;
}

const ErrorPage: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const state = (location.state as ErrorPageState) || {};

  const message =
    state.message ||
    'Something went wrong on our end. Please try again later.';

  return (
    <Container maxWidth="sm">
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="60vh">
        <Paper elevation={2} sx={{ p: 4, textAlign: 'center', width: '100%' }}>
          <ErrorIcon color="error" sx={{ fontSize: 64, mb: 2 }} />
          <Typography variant="h4" gutterBottom>
            {state.status ? `Error ${state.status}` : 'Server Error'}
          </Typography>
          <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
            {message}
          </Typography>
          <Box display="flex" justifyContent="center" gap={2}>
            <Button variant="outlined" onClick={() => navigate(-1)}>
              Go Back
            </Button>
            <Button variant="contained" onClick={() => navigate('/')}>
              Go Home
            </Button>
          </Box>
        </Paper>
      </Box>
    </Container>
  );
};

export default ErrorPage;
