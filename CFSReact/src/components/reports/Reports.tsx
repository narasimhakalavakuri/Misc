import React from 'react';
import {
  Box,
  Paper,
  Typography,
  Grid,
  Card,
  CardContent,
  Button
} from '@mui/material';
import {
  Assessment as AssessmentIcon,
  TrendingUp as TrendingUpIcon,
  PieChart as PieChartIcon
} from '@mui/icons-material';

export const Reports: React.FC = () => {
  const reports = [
    {
      title: 'Daily Position Report',
      description: 'View daily position entries and summaries',
      icon: <AssessmentIcon sx={{ fontSize: 40 }} />
    },
    {
      title: 'Transaction Summary',
      description: 'Summary of all transactions by currency',
      icon: <TrendingUpIcon sx={{ fontSize: 40 }} />
    },
    {
      title: 'Department Analysis',
      description: 'Department-wise transaction analysis',
      icon: <PieChartIcon sx={{ fontSize: 40 }} />
    }
  ];

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Reports
      </Typography>
      <Typography variant="body2" color="text.secondary" gutterBottom sx={{ mb: 3 }}>
        Generate and view various reports
      </Typography>

      <Grid container spacing={3}>
        {reports.map((report, index) => (
          <Grid item xs={12} md={4} key={index}>
            <Card>
              <CardContent>
                <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 2 }}>
                  <Box sx={{ color: 'primary.main' }}>
                    {report.icon}
                  </Box>
                  <Typography variant="h6" align="center">
                    {report.title}
                  </Typography>
                  <Typography variant="body2" color="text.secondary" align="center">
                    {report.description}
                  </Typography>
                  <Button variant="contained" fullWidth>
                    Generate Report
                  </Button>
                </Box>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>
    </Box>
  );
};