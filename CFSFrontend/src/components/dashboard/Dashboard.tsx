import React, { useEffect, useState } from 'react';
import { useSelector } from 'react-redux';
import {
  Box,
  Grid,
  Card,
  CardContent,
  Typography,
  Paper
} from '@mui/material';
import {
  PendingActions as PendingIcon,
  CheckCircle as ApprovedIcon,
  Error as IncompleteIcon,
  Edit as CorrectionIcon
} from '@mui/icons-material';
import { RootState } from '../../store';
import { positionService } from '../../service/position.service';

interface DashboardStats {
  pendingCount: number;
  approvedCount: number;
  incompleteCount: number;
  correctionCount: number;
}

export const Dashboard: React.FC = () => {
  const { user } = useSelector((state: RootState) => state.auth);
  const [stats, setStats] = useState<DashboardStats>({
    pendingCount: 0,
    approvedCount: 0,
    incompleteCount: 0,
    correctionCount: 0
  });
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadDashboardStats();
  }, [user]);

  const loadDashboardStats = async () => {
    if (!user?.department) return;

    try {
      setLoading(true);
      const today = new Date().toISOString().split('T')[0];

      const [pending, incomplete, corrections] = await Promise.all([
        positionService.getPendingApprovals(user.department, today),
        positionService.getIncompleteEntries(user.department),
        positionService.getCorrectionEntries(user.department)
      ]);

      setStats({
        pendingCount: pending.length,
        approvedCount: 0, // Would need additional endpoint
        incompleteCount: incomplete.length,
        correctionCount: corrections.length
      });
    } catch (error) {
      console.error('Failed to load dashboard stats', error);
    } finally {
      setLoading(false);
    }
  };

  const statCards = [
    {
      title: 'Pending Approval',
      count: stats.pendingCount,
      icon: <PendingIcon sx={{ fontSize: 40 }} />,
      color: '#FFA726',
      bgColor: '#FFF3E0'
    },
    {
      title: 'Approved Today',
      count: stats.approvedCount,
      icon: <ApprovedIcon sx={{ fontSize: 40 }} />,
      color: '#66BB6A',
      bgColor: '#E8F5E9'
    },
    {
      title: 'Incomplete Entries',
      count: stats.incompleteCount,
      icon: <IncompleteIcon sx={{ fontSize: 40 }} />,
      color: '#FFEE58',
      bgColor: '#FFFDE7'
    },
    {
      title: 'Corrections',
      count: stats.correctionCount,
      icon: <CorrectionIcon sx={{ fontSize: 40 }} />,
      color: '#42A5F5',
      bgColor: '#E3F2FD'
    }
  ];

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Dashboard
      </Typography>
      <Typography variant="body2" color="text.secondary" gutterBottom sx={{ mb: 3 }}>
        Welcome, {user?.fullName || user?.userid} | Department: {user?.department}
      </Typography>

      <Grid container spacing={3}>
        {statCards.map((card, index) => (
          <Grid item xs={12} sm={6} md={3} key={index}>
            <Card sx={{ bgcolor: card.bgColor }}>
              <CardContent>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <Box>
                    <Typography variant="h3" sx={{ color: card.color, fontWeight: 'bold' }}>
                      {loading ? '...' : card.count}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {card.title}
                    </Typography>
                  </Box>
                  <Box sx={{ color: card.color }}>
                    {card.icon}
                  </Box>
                </Box>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>

      <Paper sx={{ p: 3, mt: 3 }}>
        <Typography variant="h6" gutterBottom>
          Quick Actions
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Use the navigation menu on the left to access different modules.
        </Typography>
      </Paper>
    </Box>
  );
};
