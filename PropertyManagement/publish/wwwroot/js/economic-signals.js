window.economicSignals = {

    init: async function () {
        const [ecbRate, hicp, euSentiment, euribor, ireSentiment, dublinRent] = await Promise.allSettled([
            this._fetchEcbRate(),
            this._fetchHicp(),
            this._fetchEuSentiment(),
            this._fetchEuribor(),
            this._fetchIreSentiment(),
            this._fetchDublinBaseRent()
        ]);

        const rate      = ecbRate.status      === 'fulfilled' ? ecbRate.value      : null;
        const hicpVal   = hicp.status         === 'fulfilled' ? hicp.value         : null;
        const sentiment = euSentiment.status  === 'fulfilled' ? euSentiment.value  : null;
        const eur3m     = euribor.status      === 'fulfilled' ? euribor.value      : null;
        const ireSent   = ireSentiment.status === 'fulfilled' ? ireSentiment.value : null;
        const dublin    = dublinRent.status   === 'fulfilled' ? dublinRent.value   : null;

        this._updateCard('ecb-rate',      rate?.value     != null ? rate.value.toFixed(2) + '%'       : 'N/A', rate?.value     != null ? this._rateSignal(rate.value)         : 'Unavailable', rate?.date);
        this._updateCard('hicp-value',    hicpVal?.value  != null ? hicpVal.value.toFixed(1) + '%'    : 'N/A', hicpVal?.value  != null ? 'Annual inflation rate'               : 'Unavailable', hicpVal?.date);
        this._updateCard('eu-sentiment',  sentiment?.value!= null ? sentiment.value.toFixed(1)        : 'N/A', sentiment?.value!= null ? this._esiSignal(sentiment.value)      : 'Unavailable', sentiment?.date);
        this._updateCard('euribor-value', eur3m?.value    != null ? eur3m.value.toFixed(3) + '%'      : 'N/A', eur3m?.value    != null ? 'Short-term lending benchmark'        : 'Unavailable', eur3m?.date);
        this._updateCard('ire-sentiment', ireSent?.value  != null ? ireSent.value.toFixed(1)          : 'N/A', ireSent?.value  != null ? this._ireSignal(ireSent.value)        : 'Unavailable', ireSent?.date);

        this._calcSuggestedRent(rate?.value, hicpVal?.value, sentiment?.value, dublin?.value, dublin?.date);
    },

    _fetchEcbRate: async function () {
        const res = await fetch('/api/ecb/FM/M.U2.EUR.RT0.BB.B.A.A._X.A?format=jsondata&startPeriod=2024-01');
        return this._latestEcbResult(await res.json());
    },

    _fetchHicp: async function () {
        const res = await fetch('/api/ecb/ICP/M.U2.N.000000.4.ANR?format=jsondata&startPeriod=2024-01');
        return this._latestEcbResult(await res.json());
    },

    _fetchEuSentiment: async function () {
        const res = await fetch('/api/eurostat/statistics/1.0/data/ei_bssi_m_r2?lang=EN&indic=BS-ESI-I&geo=IE');
        return this._latestEurostatResult(await res.json());
    },

    _fetchEuribor: async function () {
        const res = await fetch('/api/eurostat/statistics/1.0/data/irt_st_m?lang=EN&int_rt=IRT_M3&geo=EA');
        return this._latestEurostatResult(await res.json());
    },

    _fetchIreSentiment: async function () {
        const res = await fetch('/api/eurostat/statistics/1.0/data/ei_bsco_m?lang=EN&indic=BS-CSMCI&geo=IE');
        return this._latestEurostatResult(await res.json());
    },

    _fetchDublinBaseRent: async function () {
        const res = await fetch('/api/eurostat/statistics/1.0/data/prc_colc_rents?lang=EN&currency=EUR');
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

        const geoKey = dimKeys.find(k =>
            Object.values(labelMaps[k] || {}).some(l => /dublin|berlin|paris/i.test(l))
        );
        const timeKey = dimKeys.find(k =>
            Object.keys(dims[k].category?.index || {}).some(c => /^\d{4}$/.test(c))
        );

        if (!geoKey) return null;

        const geoIdx  = dimKeys.indexOf(geoKey);
        const timeIdx = timeKey ? dimKeys.indexOf(timeKey) : -1;

        const dublinByYear = {};
        Object.keys(rawValues).map(Number).forEach(flatIdx => {
            const v = rawValues[flatIdx];
            if (v === null || v === undefined) return;
            const coords = flatIndexToCoords(flatIdx, dimSizes);
            const geo  = labelMaps[geoKey]?.[coords[geoIdx]] ?? '';
            if (!geo.toLowerCase().includes('dublin')) return;
            const year = timeIdx >= 0 ? (labelMaps[timeKey]?.[coords[timeIdx]] ?? 'unknown') : 'unknown';
            if (!dublinByYear[year]) dublinByYear[year] = [];
            dublinByYear[year].push(Math.round(v));
        });

        const latestYear = Object.keys(dublinByYear).sort().pop();
        if (!latestYear) return null;
        const vals = dublinByYear[latestYear];
        return { value: Math.round(vals.reduce((a, b) => a + b, 0) / vals.length), date: latestYear };
    },

    // ECB SDMX-JSON — returns { value, date }
    _latestEcbResult: function (data) {
        try {
            const timeDim = data.structure.dimensions.observation.find(d => d.id === 'TIME_PERIOD');
            const series  = Object.values(data.dataSets[0].series)[0];
            const obs     = series.observations;
            const sorted  = Object.keys(obs).map(Number).sort((a, b) => a - b);
            for (let i = sorted.length - 1; i >= 0; i--) {
                const v = obs[sorted[i]][0];
                if (v !== null && v !== undefined) {
                    const date = timeDim?.values?.[sorted[i]]?.id ?? null;
                    return { value: v, date: this._fmtDate(date) };
                }
            }
        } catch (e) { console.error('ECB parse error', e); }
        return null;
    },

    // Eurostat JSON-stat — returns { value, date }
    _latestEurostatResult: function (data) {
        try {
            const dims    = data.dimension || {};
            const timeKey = Object.keys(dims).find(k =>
                Object.keys(dims[k].category?.index || {}).some(c => /^\d{4}/.test(c))
            );
            const timeLabels = timeKey
                ? Object.entries(dims[timeKey].category?.index || {})
                    .reduce((acc, [id, pos]) => { acc[pos] = id; return acc; }, {})
                : {};

            const values  = data.value || {};
            const dimKeys = Object.keys(dims);
            const dimSizes = dimKeys.map(k => Object.keys(dims[k].category?.index || {}).length);
            const timeIdx  = timeKey ? dimKeys.indexOf(timeKey) : -1;

            const keys = Object.keys(values).map(Number).sort((a, b) => a - b);
            for (let i = keys.length - 1; i >= 0; i--) {
                const v = values[keys[i]];
                if (v !== null && v !== undefined) {
                    let date = null;
                    if (timeIdx >= 0) {
                        const coords = flatIndexToCoords(keys[i], dimSizes);
                        date = timeLabels[coords[timeIdx]] ?? null;
                    }
                    return { value: v, date: this._fmtDate(date) };
                }
            }
        } catch (e) { console.error('Eurostat parse error', e); }
        return null;
    },

    _fmtDate: function (raw) {
        if (!raw) return null;
        // e.g. "2025-03" → "Mar 2025", "2025" → "2025", "2025-Q1" → "Q1 2025"
        const mMatch = raw.match(/^(\d{4})-(\d{2})$/);
        if (mMatch) {
            const months = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'];
            return months[parseInt(mMatch[2]) - 1] + ' ' + mMatch[1];
        }
        const qMatch = raw.match(/^(\d{4})-Q(\d)$/);
        if (qMatch) return 'Q' + qMatch[2] + ' ' + qMatch[1];
        return raw;
    },

    _calcSuggestedRent: function (ecbRate, hicp, sentiment, baseRent, baseDate) {
        if (!baseRent) {
            const el = document.getElementById('suggested-rent');
            if (el) el.textContent = 'N/A';
            return;
        }

        const hicpGrowth      = hicp      != null ? 1 + ((hicp / 100) * 0.5)      : 1.0;
        const ecbFactor       = ecbRate   != null ? (ecbRate > 3 ? 1.02 : 1.0)    : 1.0;
        const sentimentFactor = sentiment != null ? (sentiment < 90 ? 0.98 : 1.0) : 1.0;
        const suggested       = Math.round(baseRent * hicpGrowth * ecbFactor * sentimentFactor);

        const el = document.getElementById('suggested-rent');
        if (el) el.textContent = '€' + suggested.toLocaleString();

        const breakdown = document.getElementById('rent-algo-breakdown');
        if (breakdown) {
            breakdown.innerHTML =
                `<div class="algo-row"><span>Base rent (Eurostat Dublin avg${baseDate ? ', ' + baseDate : ''})</span><span>€${baseRent.toLocaleString()}</span></div>` +
                `<div class="algo-row"><span>× HICP inflation factor${hicp != null ? ' (' + hicp.toFixed(1) + '% annual)' : ''}</span><span>${hicpGrowth.toFixed(3)}</span></div>` +
                `<div class="algo-row"><span>× ECB rate factor${ecbRate != null ? ' (' + ecbRate.toFixed(2) + '%)' : ''}</span><span>${ecbFactor.toFixed(2)}</span></div>` +
                `<div class="algo-row"><span>× Sentiment factor${sentiment != null ? ' (ESI: ' + sentiment.toFixed(1) + ')' : ''}</span><span>${sentimentFactor.toFixed(2)}</span></div>` +
                `<div class="algo-row algo-total"><span>= Suggested market rent</span><span>€${suggested.toLocaleString()}/month</span></div>`;
        }
    },

    _updateCard: function (id, value, sub, date) {
        const el = document.getElementById(id);
        if (!el) return;
        el.querySelector('.sig-value').textContent = value;
        el.querySelector('.stat-sub').textContent = sub;
        const dateEl = el.querySelector('.sig-date');
        if (dateEl) dateEl.textContent = date ? 'Data: ' + date : '';
    },

    _rateSignal:  r => r > 3   ? 'High — boosts rental demand'          : r > 1 ? 'Moderate' : 'Low',
    _esiSignal:   s => s > 100 ? 'Above average — strong demand'        : s > 80 ? 'Near average' : 'Below average',
    _ireSignal:   s => s > 0   ? 'Positive — demand healthy'            : s > -15 ? 'Cautious — moderate demand' : 'Negative — cap rent rises'
};
