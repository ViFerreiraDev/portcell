import type { ReactNode } from 'react';
import { Box, Button, Chip, Paper, Stack, Typography } from '@mui/material';

type IconName = 'grid' | 'orders' | 'users' | 'devices' | 'stock' | 'chart' | 'settings' | 'plus' | 'menu' | 'arrow' | 'search' | 'logout' | 'check' | 'clock';
const paths: Record<IconName, ReactNode> = {
  grid: <><rect x="3" y="3" width="7" height="7" rx="1.5"/><rect x="14" y="3" width="7" height="7" rx="1.5"/><rect x="3" y="14" width="7" height="7" rx="1.5"/><rect x="14" y="14" width="7" height="7" rx="1.5"/></>,
  orders: <><rect x="5" y="3" width="14" height="18" rx="2"/><path d="M9 8h6M9 12h6M9 16h4"/></>,
  users: <><circle cx="9" cy="8" r="3"/><path d="M3 20v-2a5 5 0 0 1 5-5h2a5 5 0 0 1 5 5v2M17 5a3 3 0 0 1 0 6M17 13a4 4 0 0 1 4 4v3"/></>,
  devices: <><rect x="6" y="2" width="12" height="20" rx="2"/><path d="M10 5h4M11 19h2"/></>,
  stock: <><path d="m3 7 9-4 9 4v10l-9 4-9-4zM3 7l9 4 9-4M12 11v10"/></>,
  chart: <><path d="M4 20V11M10 20V5M16 20v-8M22 20H2"/></>,
  settings: <><circle cx="12" cy="12" r="3"/><path d="m19.4 15 .1.1 1 1.7-2 2-1.8-1a8 8 0 0 1-2 .8L14 21h-4l-.6-2.4a8 8 0 0 1-2-.8l-1.8 1-2-2 1-1.8a8 8 0 0 1-.8-2L1.5 12l2.3-.6a8 8 0 0 1 .8-2l-1-1.8 2-2 1.8 1a8 8 0 0 1 2-.8L10 3h4l.6 2.4a8 8 0 0 1 2 .8l1.8-1 2 2-1 1.8a8 8 0 0 1 .8 2l2.3.6-2.3.6a8 8 0 0 1-.8 2z"/></>,
  plus: <path d="M12 5v14M5 12h14"/>,
  menu: <path d="M4 7h16M4 12h16M4 17h16"/>,
  arrow: <path d="M5 12h14m-6-6 6 6-6 6"/>,
  search: <><circle cx="10.5" cy="10.5" r="6.5"/><path d="m16 16 5 5"/></>,
  logout: <><path d="M10 3H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h5M15 7l5 5-5 5M20 12H9"/></>,
  check: <path d="m4 12 5 5L20 6"/>,
  clock: <><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></>,
};
export function Icon({ name, size = 20 }: { name: IconName; size?: number }) {
  return <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">{paths[name]}</svg>;
}

export function Brand({ light = false }: { light?: boolean }) {
  return <Stack direction="row" alignItems="center" spacing={1.25}>
    <Box sx={{ width: 36, height: 36, borderRadius: '11px', display: 'grid', placeItems: 'center', bgcolor: light ? '#a2ece5' : 'primary.main', color: light ? '#07525b' : '#fff', fontWeight: 900, fontSize: 23, letterSpacing: '-.09em' }}>P<span style={{ color: light ? '#07525b' : '#a8e9e2', marginLeft: -3 }}>·</span></Box>
    <Typography sx={{ color: light ? '#fff' : 'text.primary', fontSize: 20, fontWeight: 800, letterSpacing: '-.05em' }}>PortCell</Typography>
  </Stack>;
}

export function SectionHeader({ eyebrow, title, description, action }: { eyebrow?: string; title: string; description?: string; action?: ReactNode }) {
  return <Stack direction={{ xs: 'column', sm: 'row' }} alignItems={{ sm: 'center' }} justifyContent="space-between" gap={2} sx={{ mb: 3 }}>
    <Box>{eyebrow && <Typography sx={{ color: 'primary.main', fontSize: 11, fontWeight: 800, letterSpacing: '.13em', textTransform: 'uppercase', mb: .5 }}>{eyebrow}</Typography>}
      <Typography variant="h4">{title}</Typography>{description && <Typography color="text.secondary" sx={{ mt: .5 }}>{description}</Typography>}</Box>
    {action}
  </Stack>;
}

export function EmptyState({ icon, title, description, action }: { icon: IconName; title: string; description: string; action?: { label: string; onClick: () => void } }) {
  return <Paper sx={{ p: { xs: 3, sm: 5 }, textAlign: 'center', bgcolor: '#fbfdfd' }}>
    <Box sx={{ width: 58, height: 58, mx: 'auto', mb: 2, borderRadius: 3, bgcolor: '#e8f5f4', color: 'primary.main', display: 'grid', placeItems: 'center' }}><Icon name={icon} size={28}/></Box>
    <Typography variant="h6">{title}</Typography><Typography color="text.secondary" sx={{ maxWidth: 390, mx: 'auto', mt: .7, mb: action ? 2.5 : 0 }}>{description}</Typography>
    {action && <Button variant="contained" startIcon={<Icon name="plus" size={17}/>} onClick={action.onClick}>{action.label}</Button>}
  </Paper>;
}

const statusColors: Record<string, { bg: string; fg: string }> = {
  Recebido: { bg: '#eaf1f7', fg: '#47647e' }, AguardandoDiagnostico: { bg: '#edf1ff', fg: '#5b62a1' }, EmDiagnostico: { bg: '#e8ecfc', fg: '#535f9f' },
  AguardandoOrcamento: { bg: '#eef0f8', fg: '#666d9a' }, AguardandoAprovacao: { bg: '#fff2db', fg: '#9b681c' },
  Aprovado: { bg: '#e4f5ec', fg: '#28835f' }, EmReparo: { bg: '#e5f4f3', fg: '#087e83' }, AguardandoPeca: { bg: '#fff0e5', fg: '#ad642f' },
  ProntoParaRetirada: { bg: '#e4f5ec', fg: '#28835f' }, Entregue: { bg: '#e9f4ed', fg: '#4e8060' },
  OrcamentoRecusado: { bg: '#fcecef', fg: '#bd5768' }, Cancelado: { bg: '#f2f3f4', fg: '#66717a' },
};
export const statusLabel: Record<string, string> = {
  Recebido: 'Recebido', AguardandoDiagnostico: 'Aguardando diagnóstico', EmDiagnostico: 'Em diagnóstico', AguardandoOrcamento: 'Aguardando orçamento',
  AguardandoAprovacao: 'Aguardando aprovação', Aprovado: 'Aprovado', EmReparo: 'Em reparo', AguardandoPeca: 'Aguardando peça',
  ProntoParaRetirada: 'Pronto para retirada', Entregue: 'Entregue', OrcamentoRecusado: 'Orçamento recusado', Cancelado: 'Cancelado',
};
export function StatusChip({ status }: { status: string }) {
  const tone = statusColors[status] ?? { bg: '#edf2f3', fg: '#536975' };
  return <Chip label={statusLabel[status] ?? status} size="small" sx={{ bgcolor: tone.bg, color: tone.fg, borderRadius: 1.5, fontSize: 11.5, height: 26 }} />;
}
