import { createTheme } from '@mui/material/styles';

export const theme = createTheme({
  palette: {
    primary: { main: '#087e83', dark: '#075f66', light: '#e5f4f3' },
    secondary: { main: '#eb875e' },
    background: { default: '#f5f7f7', paper: '#ffffff' },
    text: { primary: '#172b3d', secondary: '#66788a' },
    divider: '#e5ebec',
    success: { main: '#2c936c' },
    warning: { main: '#c88227' },
    error: { main: '#c55255' },
  },
  shape: { borderRadius: 14 },
  typography: {
    fontFamily: 'Inter, "Segoe UI", Arial, sans-serif',
    h4: { fontSize: '1.9rem', fontWeight: 750, letterSpacing: '-0.035em' },
    h5: { fontSize: '1.4rem', fontWeight: 730, letterSpacing: '-0.025em' },
    h6: { fontSize: '1.08rem', fontWeight: 720 },
    subtitle1: { fontWeight: 650 },
    button: { fontWeight: 700, textTransform: 'none', letterSpacing: 0 },
  },
  components: {
    MuiCssBaseline: { styleOverrides: {
      body: { background: '#f5f7f7' },
      '*': { boxSizing: 'border-box' },
      '::selection': { background: '#bce9e5' },
    } },
    MuiPaper: { styleOverrides: { root: { border: '1px solid #e5ebec', boxShadow: '0 10px 35px rgba(18, 44, 58, 0.035)' } } },
    MuiButton: { styleOverrides: { root: { borderRadius: 10, minHeight: 40, boxShadow: 'none' }, contained: { boxShadow: '0 5px 14px rgba(8, 126, 131, 0.16)', '&:hover': { boxShadow: '0 7px 18px rgba(8, 126, 131, 0.22)' } } } },
    MuiOutlinedInput: { styleOverrides: { root: { borderRadius: 10, background: '#fff', '&:hover .MuiOutlinedInput-notchedOutline': { borderColor: '#8daeb0' }, '& input:-webkit-autofill': { WebkitBoxShadow: '0 0 0 100px #fff inset', WebkitTextFillColor: '#172b3d' } } } },
    MuiAlert: { styleOverrides: { root: { borderRadius: 12 } } },
    MuiChip: { styleOverrides: { root: { fontWeight: 700 } } },
  },
});
