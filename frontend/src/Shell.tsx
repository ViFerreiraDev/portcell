import { useState, type FormEvent, type ReactNode } from 'react';
import { Alert, Box, Button, Drawer, IconButton, Paper, Stack, TextField, Typography } from '@mui/material';
import { Brand, Icon } from './ui';

export type Page = 'dashboard' | 'clientes' | 'aparelhos' | 'ordens' | 'estoque' | 'relatorios' | 'admin';
const navigation: { id: Page; label: string; icon: 'grid' | 'orders' | 'users' | 'devices' | 'stock' | 'chart' | 'settings'; roles?: string[] }[] = [
  { id: 'dashboard', label: 'Visão geral', icon: 'grid' },
  { id: 'ordens', label: 'Ordens de serviço', icon: 'orders' },
  { id: 'clientes', label: 'Clientes', icon: 'users' },
  { id: 'aparelhos', label: 'Aparelhos', icon: 'devices' },
  { id: 'estoque', label: 'Estoque', icon: 'stock' },
  { id: 'relatorios', label: 'Relatórios', icon: 'chart', roles: ['Administrador', 'Gerente'] },
  { id: 'admin', label: 'Administração', icon: 'settings', roles: ['Administrador'] },
];
const pageNames = Object.fromEntries(navigation.map(x => [x.id, x.label]));

export function LoginScreen({ email, password, error, onEmail, onPassword, onSubmit }: {
  email: string; password: string; error: string; onEmail: (value: string) => void; onPassword: (value: string) => void; onSubmit: (event: FormEvent) => void;
}) {
  return <Box sx={{ minHeight: '100vh', display: 'grid', gridTemplateColumns: { xs: '1fr', md: 'minmax(400px, 47%) 1fr' }, bgcolor: '#f7f9f9' }}>
    <Box sx={{ display: { xs: 'none', md: 'flex' }, flexDirection: 'column', position: 'relative', overflow: 'hidden', p: { md: 5, lg: 7 }, color: '#fff', bgcolor: '#102f40', backgroundImage: 'radial-gradient(circle at 90% 82%, rgba(19,144,144,.52), transparent 45%), radial-gradient(circle at 0 0, rgba(80,135,157,.25), transparent 36%)' }}>
      <Brand light />
      <Box sx={{ position: 'relative', zIndex: 1, mt: 'auto', mb: 5 }}>
        <Typography sx={{ color: '#8ce0d7', fontSize: 12, fontWeight: 800, letterSpacing: '.16em', textTransform: 'uppercase', mb: 2 }}>Sua operação em ordem</Typography>
        <Typography sx={{ fontSize: { md: 39, lg: 50 }, lineHeight: 1.1, fontWeight: 780, letterSpacing: '-.055em', maxWidth: 520 }}>Da bancada à entrega, tudo no mesmo lugar.</Typography>
        <Typography sx={{ color: '#b8d0d7', mt: 2.5, maxWidth: 460, lineHeight: 1.7 }}>Acompanhe cada aparelho, organize sua equipe e dê visibilidade ao que importa para o cliente.</Typography>
        <Stack direction="row" spacing={1} sx={{ mt: 4 }}><Box sx={{ width: 34, height: 4, borderRadius: 2, bgcolor: '#8ce0d7' }}/><Box sx={{ width: 16, height: 4, borderRadius: 2, bgcolor: '#517b86' }}/><Box sx={{ width: 16, height: 4, borderRadius: 2, bgcolor: '#517b86' }}/></Stack>
      </Box>
      <Typography sx={{ color: '#8caeb8', fontSize: 12 }}>PORTCELL · GESTÃO DE ASSISTÊNCIA TÉCNICA</Typography>
      <Box sx={{ position: 'absolute', width: 360, height: 360, right: -115, top: '17%', border: '1px solid rgba(157,222,219,.16)', borderRadius: '50%' }} />
      <Box sx={{ position: 'absolute', width: 245, height: 245, right: -55, top: '25%', border: '1px solid rgba(157,222,219,.22)', borderRadius: '50%' }} />
    </Box>
    <Box sx={{ minWidth: 0, display: 'flex', flexDirection: 'column', px: { xs: 2.5, sm: 5, lg: 9 }, py: { xs: 3, sm: 5 }, justifyContent: 'center' }}>
      <Box sx={{ display: { xs: 'block', md: 'none' }, mb: { xs: 6, sm: 8 } }}><Brand /></Box>
      <Box sx={{ width: '100%', maxWidth: 430, mx: 'auto' }}>
        <Typography sx={{ color: 'primary.main', fontSize: 11, fontWeight: 800, letterSpacing: '.13em', mb: 1.3 }}>BEM-VINDO DE VOLTA</Typography>
        <Typography variant="h4" sx={{ fontSize: { xs: 31, sm: 37 }, mb: 1 }}>Acesse sua operação</Typography>
        <Typography color="text.secondary" sx={{ mb: 4 }}>Entre com sua conta para acompanhar a assistência técnica.</Typography>
        <Paper component="form" onSubmit={onSubmit} sx={{ p: { xs: 2.5, sm: 4 }, borderRadius: 4, boxShadow: '0 22px 70px rgba(18, 44, 58, .08)' }}>
          <Stack spacing={2.5}>
            {error && <Alert severity="error">{error}</Alert>}
            <TextField label="E-mail" type="email" autoComplete="username" InputLabelProps={{ shrink: true }} required fullWidth value={email} onChange={e => onEmail(e.target.value)} />
            <TextField label="Senha" type="password" autoComplete="current-password" InputLabelProps={{ shrink: true }} required fullWidth value={password} onChange={e => onPassword(e.target.value)} />
            <Button type="submit" variant="contained" size="large" fullWidth endIcon={<Icon name="arrow" size={18}/>} sx={{ minHeight: 48 }}>Entrar no PortCell</Button>
          </Stack>
        </Paper>
        <Typography color="text.secondary" sx={{ mt: 3, fontSize: 12, textAlign: 'center' }}>Acesso restrito à equipe autorizada.</Typography>
      </Box>
    </Box>
  </Box>;
}

export function Shell({ page, setPage, profile, name, onLogout, children }: { page: Page; setPage: (page: Page) => void; profile: string; name: string; onLogout: () => void; children: ReactNode }) {
  const [menuOpen, setMenuOpen] = useState(false);
  const select = (value: Page) => { setPage(value); setMenuOpen(false); window.scrollTo({ top: 0, behavior: 'smooth' }); };
  const nav = <Box sx={{ width: 250, height: '100%', display: 'flex', flexDirection: 'column', bgcolor: '#132f40', color: '#fff', px: 2.1, pt: 3, pb: 2.5 }}>
    <Box sx={{ px: 1, mb: 6 }}><Brand light /></Box>
    <Typography sx={{ color: '#8aaab4', fontSize: 10, fontWeight: 800, letterSpacing: '.15em', px: 1.5, mb: 1.5 }}>MENU PRINCIPAL</Typography>
    <Stack spacing={.5}>
      {navigation.filter(item => !item.roles || item.roles.includes(profile)).map(item => <Button key={item.id} onClick={() => select(item.id)} startIcon={<Icon name={item.icon} size={19}/>} sx={{ justifyContent: 'flex-start', textAlign: 'left', px: 1.5, minHeight: 45, borderRadius: 2.2, color: page === item.id ? '#fff' : '#bbd0d6', bgcolor: page === item.id ? 'rgba(72, 187, 180, .18)' : 'transparent', '& .MuiButton-startIcon': { mr: 1.4, color: page === item.id ? '#80ded4' : '#91aab5' }, '&:hover': { bgcolor: 'rgba(255,255,255,.08)' } }}>{item.label}</Button>)}
    </Stack>
    <Box sx={{ mt: 'auto', pt: 2, borderTop: '1px solid rgba(255,255,255,.1)' }}>
      <Stack direction="row" spacing={1.2} alignItems="center" sx={{ px: 1, mb: 2 }}><Box sx={{ width: 34, height: 34, borderRadius: '50%', bgcolor: '#d6f1ec', color: '#175a61', display: 'grid', placeItems: 'center', fontWeight: 800 }}>{name[0]?.toUpperCase() ?? 'P'}</Box><Box sx={{ minWidth: 0 }}><Typography sx={{ fontWeight: 700, fontSize: 13 }} noWrap>{name}</Typography><Typography sx={{ color: '#a1b9c1', fontSize: 11 }}>{profile}</Typography></Box></Stack>
      <Button onClick={onLogout} startIcon={<Icon name="logout" size={17}/>} sx={{ color: '#bbd0d6', width: '100%', justifyContent: 'flex-start', px: 1.5 }}>Sair da conta</Button>
    </Box>
  </Box>;
  return <Box sx={{ display: 'flex', minHeight: '100vh' }}>
    <Box component="aside" sx={{ display: { xs: 'none', md: 'block' }, width: 250, flexShrink: 0, position: 'sticky', top: 0, height: '100vh' }}>{nav}</Box>
    <Drawer open={menuOpen} onClose={() => setMenuOpen(false)} sx={{ display: { md: 'none' } }} PaperProps={{ sx: { border: 0 } }}>{nav}</Drawer>
    <Box sx={{ flex: 1, minWidth: 0 }}>
      <Box component="header" sx={{ height: 70, px: { xs: 2, sm: 3, lg: 5 }, bgcolor: '#fff', borderBottom: '1px solid #e5ebec', display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 2 }}>
        <Stack direction="row" alignItems="center" spacing={1.5} sx={{ minWidth: 0 }}><IconButton onClick={() => setMenuOpen(true)} aria-label="Abrir menu" sx={{ display: { md: 'none' }, color: 'text.primary' }}><Icon name="menu"/></IconButton><Typography sx={{ color: 'text.secondary', fontSize: 13, display: { xs: 'none', sm: 'block' } }}>Área de trabalho</Typography><Typography sx={{ color: '#a9b7c0', display: { xs: 'none', sm: 'block' } }}>/</Typography><Typography sx={{ fontSize: 13, fontWeight: 750 }} noWrap>{pageNames[page]}</Typography></Stack>
        <Stack direction="row" alignItems="center" spacing={1.5}><Box sx={{ display: { xs: 'none', sm: 'block' }, textAlign: 'right' }}><Typography sx={{ fontSize: 12, fontWeight: 750 }}>{name}</Typography><Typography sx={{ fontSize: 11, color: 'text.secondary' }}>{profile}</Typography></Box><Box sx={{ width: 34, height: 34, borderRadius: '50%', bgcolor: '#e4f3f1', color: 'primary.dark', display: 'grid', placeItems: 'center', fontWeight: 800, fontSize: 13 }}>{name[0]?.toUpperCase() ?? 'P'}</Box></Stack>
      </Box>
      <Box component="main" sx={{ maxWidth: 1460, mx: 'auto', px: { xs: 2, sm: 3, lg: 5 }, py: { xs: 3, lg: 4.5 } }}>{children}</Box>
    </Box>
  </Box>;
}
