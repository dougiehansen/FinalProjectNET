window.revenueChart = {
    _chart: null,

    init: function (canvasId, labels, amounts, expected) {
        if (this._chart) { this._chart.destroy(); this._chart = null; }

        const canvas = document.getElementById(canvasId);
        if (!canvas) return;

        const maxAmount = Math.max(...amounts, expected);
        const barColours = amounts.map(v =>
            v === 0
                ? 'rgba(148,163,184,0.5)'
                : v >= expected
                    ? 'rgba(22,163,74,0.75)'
                    : 'rgba(37,99,235,0.75)'
        );

        this._chart = new Chart(canvas.getContext('2d'), {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [
                    {
                        label: 'Rent Collected',
                        data: amounts,
                        backgroundColor: barColours,
                        borderColor: barColours.map(c => c.replace('0.75', '1').replace('0.5', '0.8')),
                        borderWidth: 1,
                        borderRadius: 6,
                        order: 2
                    },
                    {
                        label: `Expected (€${expected.toLocaleString()}/mo)`,
                        data: labels.map(() => expected),
                        type: 'line',
                        borderColor: '#f59e0b',
                        backgroundColor: 'transparent',
                        borderWidth: 2,
                        borderDash: [6, 4],
                        pointRadius: 0,
                        tension: 0,
                        order: 1
                    }
                ]
            },
            options: {
                responsive: true,
                plugins: {
                    legend: {
                        position: 'top',
                        labels: { font: { family: 'Inter, sans-serif', size: 12 }, boxWidth: 14 }
                    },
                    tooltip: {
                        callbacks: {
                            label: ctx => ` €${ctx.parsed.y?.toLocaleString()}`
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: val => `€${val.toLocaleString()}`,
                            font: { family: 'Inter, sans-serif', size: 11 }
                        },
                        grid: { color: '#f3f4f6' }
                    },
                    x: {
                        ticks: { font: { family: 'Inter, sans-serif', size: 11 } },
                        grid: { display: false }
                    }
                }
            }
        });
    }
};
