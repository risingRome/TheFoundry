const foundryCharts = [];

function chartTheme() {
    const styles = getComputedStyle(document.documentElement);
    return {
        text: styles.getPropertyValue('--foundry-muted').trim(),
        grid: styles.getPropertyValue('--foundry-border').trim(),
        primary: styles.getPropertyValue('--foundry-primary').trim()
    };
}

function commonOptions() {
    const theme = chartTheme();
    return {
        responsive: true,
        maintainAspectRatio: false,
        interaction: { intersect: false, mode: 'index' },
        plugins: {
            legend: { display: false },
            tooltip: {
                backgroundColor: '#111827',
                titleFont: { family: 'Inter', size: 11 },
                bodyFont: { family: 'Inter', size: 11 },
                padding: 10,
                cornerRadius: 6
            }
        },
        scales: {
            x: {
                grid: { display: false },
                ticks: { color: theme.text, font: { family: 'Inter', size: 10 }, maxRotation: 0, autoSkip: true, maxTicksLimit: 8 },
                border: { display: false }
            },
            y: {
                beginAtZero: true,
                grid: { color: theme.grid, drawTicks: false },
                ticks: { color: theme.text, font: { family: 'Inter', size: 10 }, padding: 8 },
                border: { display: false }
            }
        }
    };
}

function renderLineChart(id, labels, values, label) {
    const theme = chartTheme();
    const chart = new Chart(document.getElementById(id), {
        type: 'line',
        data: {
            labels,
            datasets: [{
                label,
                data: values,
                borderColor: theme.primary,
                backgroundColor: `${theme.primary}18`,
                borderWidth: 2,
                pointRadius: 0,
                pointHoverRadius: 4,
                pointBackgroundColor: theme.primary,
                tension: .35,
                fill: true
            }]
        },
        options: commonOptions()
    });
    foundryCharts.push(chart);
}

function renderBarChart(id, labels, values, label) {
    const theme = chartTheme();
    const options = commonOptions();
    options.scales.y.ticks.precision = 0;
    const chart = new Chart(document.getElementById(id), {
        type: 'bar',
        data: {
            labels,
            datasets: [{ label, data: values, backgroundColor: [theme.primary, '#16805a', '#b26a00', '#c03a31'], borderRadius: 4, borderSkipped: false, maxBarThickness: 44 }]
        },
        options
    });
    foundryCharts.push(chart);
}

window.addEventListener('foundry:themechange', () => {
    const theme = chartTheme();
    foundryCharts.forEach(chart => {
        chart.options.scales.x.ticks.color = theme.text;
        chart.options.scales.y.ticks.color = theme.text;
        chart.options.scales.y.grid.color = theme.grid;
        chart.data.datasets[0].borderColor = theme.primary;
        if (chart.config.type === 'line') chart.data.datasets[0].backgroundColor = `${theme.primary}18`;
        chart.update();
    });
});
