import { useEffect, useState } from 'react';
import { Alert, Box, Button, Paper, Skeleton, Stack, Typography } from '@mui/material';
import { request, type Aparelho, type Cliente, type Ordem } from './api';
import { EmptyState, Icon, SectionHeader, StatusChip } from './ui';

type Summary = { abertas: number; porStatus: { status: string; count: number }[]; faturamento: number | null; ticketMedio: number | null };
const money = (value: number) => value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
const queue = [
  { status: 'AguardandoAprovacao', label: 'Aguardando aprovação', accent: '#dc9b39' },
  { status: 'EmReparo', label: 'Em reparo', accent: '#258e92' },
  { status: 'AguardandoPeca', label: 'Aguardando peça', accent: '#da7954' },
  { status: 'ProntoParaRetirada', label: 'Prontas para retirada', accent: '#4a9e73' },
];

export function Dashboard({ token, orders, clients, clientTotal, devices, onOrders, onNewOrder, onSelectOrder }: {
  token: string; orders: Ordem[]; clients: Cliente[]; clientTotal: number; devices: Aparelho[];
  onOrders: () => void; onNewOrder: () => void; onSelectOrder: (order: Ordem) => void;
}) {
  const [summary, setSummary] = useState<Summary | null>(null);
  const [error, setError] = useState('');
  useEffect(() => { request<Summary>('/api/dashboard', token).then(setSummary).catch(error => setError(String(error))); }, [token]);
  const attention = queue.reduce((total, item) => total + (summary?.porStatus.find(x => x.status === item.status)?.count ?? 0), 0);
  return <>
    <SectionHeader eyebrow="Painel operacional" title="Visão geral" description="Acompanhe a operação e veja o que precisa de atenção agora." action={<Button variant="contained" startIcon={<Icon name="plus" size={18}/>} onClick={onNewOrder}>Nova ordem de serviço</Button>} />
    {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
    <Box sx={{ display: 'grid', gridTemplateColumns: { xs: 'repeat(2, minmax(0, 1fr))', xl: 'repeat(4, 1fr)' }, gap: 2, mb: 3 }}>
      <Metric label="Ordens em aberto" value={summary?.abertas} icon="orders" tone="#e8f4f3" color="#087e83" />
      <Metric label="Na fila de atenção" value={attention} icon="clock" tone="#fff2e5" color="#bb722f" loading={!summary} />
      <Metric label="Clientes cadastrados" value={clientTotal} icon="users" tone="#edf0ff" color="#6874b5" />
      <Metric label="Aparelhos cadastrados" value={devices.length} icon="devices" tone="#edf3fa" color="#4b80a5" />
    </Box>
    <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', xl: 'minmax(0, 1.6fr) minmax(310px, 1fr)' }, gap: 3, alignItems: 'start' }}>
      <Paper sx={{ overflow: 'hidden' }}>
        <Stack direction="row" alignItems="center" justifyContent="space-between" sx={{ px: 3, py: 2.5, borderBottom: '1px solid', borderColor: 'divider' }}><Box><Typography variant="h6">Ordens recentes</Typography><Typography color="text.secondary" sx={{ fontSize: 13, mt: .3 }}>Últimas movimentações da assistência</Typography></Box><Button size="small" endIcon={<Icon name="arrow" size={16}/>} onClick={onOrders}>Ver todas</Button></Stack>
        {orders.length === 0 ? <Box sx={{ p: 2.5 }}><EmptyState icon="orders" title="Nenhuma ordem por enquanto" description="Abra sua primeira OS para acompanhar o serviço desde a entrada do aparelho." action={{ label: 'Abrir OS', onClick: onNewOrder }} /></Box> : orders.slice(0, 6).map((order, index) => {
          const client = clients.find(x => x.id === order.clienteId);
          const device = devices.find(x => x.id === order.aparelhoId);
          return <Box key={order.id} component="button" onClick={() => onSelectOrder(order)} sx={{ width: '100%', border: 0, borderBottom: index < Math.min(orders.length, 6) - 1 ? '1px solid #edf0f1' : 0, bgcolor: 'transparent', px: 3, py: 2, display: 'flex', alignItems: 'center', gap: 2, textAlign: 'left', cursor: 'pointer', '&:hover': { bgcolor: '#f8fbfb' } }}>
            <Box sx={{ width: 41, height: 41, borderRadius: 2.2, bgcolor: '#ebf4f4', color: 'primary.main', display: 'grid', placeItems: 'center', flexShrink: 0 }}><Icon name="devices" size={20}/></Box>
            <Box sx={{ flex: 1, minWidth: 0 }}><Typography sx={{ fontSize: 13.5, fontWeight: 750 }}>OS #{order.numero} <Box component="span" sx={{ color: 'text.secondary', fontWeight: 450, ml: .6 }}>{device ? `${device.marca} ${device.modelo}` : ''}</Box></Typography><Typography color="text.secondary" noWrap sx={{ fontSize: 12.5, mt: .3 }}>{client?.nome ?? 'Cliente'} · {order.defeitoRelatado}</Typography></Box>
            <Box sx={{ display: { xs: 'none', sm: 'block' } }}><StatusChip status={order.status}/></Box><Box sx={{ color: '#9cafb5' }}><Icon name="arrow" size={17}/></Box>
          </Box>;
        })}
      </Paper>
      <Stack spacing={3}>
        <Paper sx={{ p: 3 }}><Stack direction="row" alignItems="center" justifyContent="space-between"><Typography variant="h6">Fila de atenção</Typography><Box sx={{ width: 33, height: 33, borderRadius: 2, bgcolor: '#eaf5f4', color: 'primary.main', display: 'grid', placeItems: 'center' }}><Icon name="clock" size={18}/></Box></Stack><Typography color="text.secondary" sx={{ fontSize: 13, mt: .5, mb: 2 }}>Etapas que pedem uma próxima ação.</Typography>
          {queue.map((item, index) => <Stack key={item.status} direction="row" alignItems="center" spacing={1.5} sx={{ py: 1.25, borderBottom: index < queue.length - 1 ? '1px solid #edf0f1' : 0 }}><Box sx={{ width: 8, height: 8, borderRadius: '50%', bgcolor: item.accent }}/><Typography sx={{ flex: 1, fontSize: 13 }}>{item.label}</Typography><Typography sx={{ fontWeight: 800, fontSize: 15 }}>{summary ? summary.porStatus.find(x => x.status === item.status)?.count ?? 0 : '—'}</Typography></Stack>)}
        </Paper>
        {summary?.faturamento !== null && summary?.faturamento !== undefined && <Paper sx={{ p: 3, background: 'linear-gradient(145deg, #123848, #0e6c70)', color: '#fff', border: 0 }}><Typography sx={{ fontSize: 12, color: '#acd6d5', fontWeight: 700 }}>FATURAMENTO REGISTRADO</Typography><Typography sx={{ fontSize: 31, fontWeight: 800, letterSpacing: '-.04em', mt: 1 }}>{money(summary.faturamento)}</Typography><Typography sx={{ fontSize: 12, color: '#c2e1df', mt: 1.5 }}>Ticket médio: {money(summary.ticketMedio ?? 0)}</Typography></Paper>}
      </Stack>
    </Box>
  </>;
}

function Metric({ label, value, icon, tone, color, loading = false }: { label: string; value?: number; icon: 'orders' | 'clock' | 'users' | 'devices'; tone: string; color: string; loading?: boolean }) {
  return <Paper sx={{ p: { xs: 2, sm: 2.5 }, minHeight: 126 }}><Stack direction="row" justifyContent="space-between" alignItems="flex-start" spacing={.5}><Typography color="text.secondary" sx={{ fontSize: 12.5, fontWeight: 650 }}>{label}</Typography><Box sx={{ width: 34, height: 34, flexShrink: 0, borderRadius: 2.2, bgcolor: tone, color, display: 'grid', placeItems: 'center' }}><Icon name={icon} size={18}/></Box></Stack><Typography sx={{ fontSize: 32, fontWeight: 800, letterSpacing: '-.05em', lineHeight: 1, mt: 2 }}>{loading || value === undefined ? <Skeleton width={45}/> : value}</Typography></Paper>;
}
