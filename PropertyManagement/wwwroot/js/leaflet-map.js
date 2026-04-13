window.leafletMap = {
    _map: null,

    init: function (elementId, properties, dotNetRef) {
        if (this._map) {
            this._map.remove();
            this._map = null;
        }

        const map = L.map(elementId).setView([53.3461, -6.2675], 13);
        this._map = map;

        L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png', {
            attribution: '© OpenStreetMap contributors © CARTO',
            subdomains: 'abcd',
            maxZoom: 19
        }).addTo(map);

        const icon = L.divIcon({
            className: '',
            html: `<div style="background:#2563eb;width:36px;height:36px;border-radius:50%;border:3px solid white;box-shadow:0 2px 8px rgba(0,0,0,0.25);display:flex;align-items:center;justify-content:center;cursor:pointer;pointer-events:auto;">
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="white" width="18" height="18" style="pointer-events:none;">
                    <path d="M10 20v-6h4v6h5v-8h3L12 3 2 12h3v8z"/>
                </svg>
            </div>`,
            iconSize: [36, 36],
            iconAnchor: [18, 18]
        });

        if (properties.length === 0) {
            const msg = L.divIcon({
                className: '',
                html: '<div style="background:#fff;padding:8px 14px;border-radius:8px;box-shadow:0 2px 8px rgba(0,0,0,0.18);font-size:13px;color:#6b7280;white-space:nowrap;">No properties with coordinates yet</div>',
                iconAnchor: [110, 16]
            });
            L.marker([53.3461, -6.2675], { icon: msg }).addTo(map);
            return;
        }

        properties.forEach(function (p) {
            L.marker([p.latitude, p.longitude], { icon })
                .addTo(map)
                .on('click', function () {
                    dotNetRef.invokeMethodAsync('ShowProperty', p.name);
                });
        });
    }
};
