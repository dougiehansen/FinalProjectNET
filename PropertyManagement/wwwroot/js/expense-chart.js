window.expenseChart = {
    _chart: null,

    init: function (canvasId, labels, amounts, colours) {
        if (this._chart) { this._chart.destroy(); this._chart = null; }

        const canvas = document.getElementById(canvasId);
        if (!canvas || !amounts.length) return;

        const total = amounts.reduce((a, b) => a + b, 0);

        this._chart = new Chart(canvas.getContext('2d'), {
            type: 'doughnut',
            data: {
                labels: labels,
                datasets: [{
                    data: amounts,
                    backgroundColor: colours.map(c => c + '33'),
                    borderColor: colours,
                    borderWidth: 2,
                    hoverBackgroundColor: colours.map(c => c + '66'),
                    hoverBorderWidth: 3
                }]
            },
            options: {
                responsive: true,
                cutout: '62%',
                plugins: {
                    legend: {
                        position: 'right',
                        labels: {
                            font: { family: 'Inter, sans-serif', size: 12 },
                            boxWidth: 13,
                            padding: 14,
                            generateLabels: chart => {
                                const ds = chart.data.datasets[0];
                                return chart.data.labels.map((label, i) => ({
                                    text: `${label}  €${ds.data[i].toLocaleString()}`,
                                    fillStyle: ds.backgroundColor[i],
                                    strokeStyle: ds.borderColor[i],
                                    lineWidth: 2,
                                    index: i
                                }));
                            }
                        }
                    },
                    tooltip: {
                        callbacks: {
                            label: ctx => {
                                const pct = total > 0 ? Math.round(ctx.parsed / total * 100) : 0;
                                return `  €${ctx.parsed.toLocaleString()} — ${pct}%`;
                            }
                        }
                    }
                }
            }
        });
    }
};
