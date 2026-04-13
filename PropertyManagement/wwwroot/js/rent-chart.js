window.rentChart = {
    _chart: null,
    _cityData: {},   // { city: { buildingType: avgValue } }
    _cities: [],

    init: async function (canvasId, portfolioAvg) {
        if (this._chart) { this._chart.destroy(); this._chart = null; }

        const canvas = document.getElementById(canvasId);
        if (!canvas) return;

        const statusEl = canvas.parentElement.querySelector('.chart-status');
        const filterEl = document.getElementById('building-filter');
        statusEl.textContent = 'Loading live data from Eurostat…';
        if (filterEl) filterEl.disabled = true;

        this._portfolioAvg = portfolioAvg;
        this._canvasId = canvasId;

        const url = '/api/eurostat/statistics/1.0/data/prc_colc_rents?lang=EN&currency=EUR&time=2023';

        const targets = [
            'Dublin', 'Amsterdam', 'Berlin', 'Munich', 'Hamburg',
            'Paris', 'Lyon', 'Madrid', 'Barcelona', 'Lisbon', 'Porto',
            'Rome', 'Milan', 'Vienna', 'Brussels', 'Stockholm',
            'Copenhagen', 'Helsinki', 'Prague', 'Warsaw', 'Athens'
        ];

        try {
            const res = await fetch(url);
            if (!res.ok) throw new Error('HTTP ' + res.status);
            const data = await res.json();

            const rawValues = data.value || {};
            const dims = data.dimension || {};
            const dimKeys = Object.keys(dims);

            const labelMaps = {};
            const dimSizes = [];
            dimKeys.forEach(k => {
                const catIdx = dims[k].category?.index || {};
                const catLbl = dims[k].category?.label || {};
                const map = {};
                Object.entries(catIdx).forEach(([code, pos]) => { map[pos] = catLbl[code] || code; });
                labelMaps[k] = map;
                dimSizes.push(Object.keys(catIdx).length);
            });

            const geoKey = dimKeys.find(k => {
                const labels = Object.values(labelMaps[k] || {});
                return labels.some(l => /dublin|berlin|paris|madrid|rome|amsterdam/i.test(l));
            }) || 'geo';

            const buildingKey = dimKeys.find(k =>
                k !== geoKey && k !== 'freq' && k !== 'time' && k !== 'currency'
            );

            const geoIdx = dimKeys.indexOf(geoKey);
            const buildingIdx = buildingKey ? dimKeys.indexOf(buildingKey) : -1;

            // Parse all rows into cityData[city][buildingType] = [values]
            const cityData = {};
            const buildingTypes = new Set();

            Object.keys(rawValues).map(Number).forEach(flatIdx => {
                const v = rawValues[flatIdx];
                if (v === null || v === undefined) return;
                const coords = flatIndexToCoords(flatIdx, dimSizes);
                const geo = labelMaps[geoKey]?.[coords[geoIdx]] ?? '–';
                const building = buildingIdx >= 0 ? (labelMaps[buildingKey]?.[coords[buildingIdx]] ?? '–') : 'All';

                const match = targets.find(t => geo.toLowerCase().includes(t.toLowerCase()));
                if (!match) return;

                buildingTypes.add(building);
                if (!cityData[match]) cityData[match] = {};
                if (!cityData[match][building]) cityData[match][building] = [];
                cityData[match][building].push(Math.round(v));
            });

            // Average each city/building combination
            this._cityData = {};
            Object.entries(cityData).forEach(([city, types]) => {
                this._cityData[city] = {};
                Object.entries(types).forEach(([type, vals]) => {
                    this._cityData[city][type] = Math.round(vals.reduce((a, b) => a + b, 0) / vals.length);
                });
            });

            this._cities = targets.filter(t => this._cityData[t]);

            // Populate filter dropdown
            if (filterEl) {
                filterEl.innerHTML = '<option value="__all__">All dwelling types (average)</option>';
                [...buildingTypes].sort().forEach(type => {
                    const opt = document.createElement('option');
                    opt.value = type;
                    opt.textContent = type;
                    filterEl.appendChild(opt);
                });
                filterEl.disabled = false;
                filterEl.onchange = () => this.filter(filterEl.value);
            }

            statusEl.textContent = '';

        } catch (err) {
            console.error('Eurostat fetch failed:', err);
            statusEl.textContent = 'Could not load Eurostat data — showing estimates.';

            this._cities = ['Dublin', 'Amsterdam', 'Paris', 'Berlin', 'Madrid', 'Lisbon', 'Rome', 'Vienna'];
            const fallback = [2200, 2100, 1800, 1400, 1200, 1350, 1500, 1300];
            this._cityData = {};
            this._cities.forEach((c, i) => { this._cityData[c] = { 'All': fallback[i] }; });

            if (filterEl) filterEl.disabled = true;
        }

        this._drawChart('__all__');
    },

    filter: function (buildingType) {
        this._drawChart(buildingType);
    },

    _drawChart: function (buildingType) {
        if (this._chart) { this._chart.destroy(); this._chart = null; }

        const canvas = document.getElementById(this._canvasId);
        if (!canvas) return;

        const values = this._cities.map(city => {
            const types = this._cityData[city] || {};
            if (buildingType === '__all__') {
                const vals = Object.values(types);
                return vals.length ? Math.round(vals.reduce((a, b) => a + b, 0) / vals.length) : null;
            }
            return types[buildingType] ?? null;
        });

        const max = Math.max(...values.filter(Boolean));
        const colours = values.map(v => v === max ? 'rgba(220,38,38,0.75)' : 'rgba(37,99,235,0.75)');
        const portfolioAvg = this._portfolioAvg;

        this._chart = new Chart(canvas.getContext('2d'), {
            type: 'bar',
            data: {
                labels: this._cities,
                datasets: [
                    {
                        label: 'European City Average €/month (Eurostat 2023)',
                        data: values,
                        backgroundColor: colours,
                        borderColor: colours.map(c => c.replace('0.75', '1')),
                        borderWidth: 1,
                        borderRadius: 8
                    },
                    {
                        label: `Our Portfolio Average — €${portfolioAvg}/month`,
                        data: this._cities.map(() => portfolioAvg),
                        type: 'line',
                        borderColor: '#16a34a',
                        backgroundColor: 'transparent',
                        borderWidth: 2.5,
                        borderDash: [6, 4],
                        pointRadius: 0,
                        tension: 0
                    }
                ]
            },
            options: {
                responsive: true,
                plugins: {
                    legend: { position: 'top', labels: { font: { family: 'Inter, sans-serif', size: 13 } } },
                    tooltip: { callbacks: { label: ctx => ` €${ctx.parsed.y?.toLocaleString()}/month` } }
                },
                scales: {
                    y: {
                        beginAtZero: false,
                        min: 600,
                        ticks: { callback: val => `€${val.toLocaleString()}`, font: { family: 'Inter, sans-serif' } },
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

function flatIndexToCoords(flatIdx, sizes) {
    const coords = new Array(sizes.length);
    let remaining = flatIdx;
    for (let i = sizes.length - 1; i >= 0; i--) {
        coords[i] = remaining % sizes[i];
        remaining = Math.floor(remaining / sizes[i]);
    }
    return coords;
}
