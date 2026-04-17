import { AfterViewInit, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as L from 'leaflet';
import { kml as parseKml } from '@mapbox/togeojson';

interface Country {
  name: string;
  code: string;
  capital: string;
}

@Component({
  selector: 'app-world-map',
  standalone: false,
  templateUrl: './world-map.component.html',
  styleUrls: ['./world-map.component.css'],
})
export class WorldMapComponent implements OnInit, AfterViewInit {
  @ViewChild('mapContainer', { static: false }) mapContainer!: ElementRef<HTMLDivElement>;

  countries: Country[] = [];
  visitedCountries: Set<string> = new Set();
  countryNameToCode: Map<string, string> = new Map();
  mapError: string | null = null;

  private map!: L.Map;
  private kmlLayer?: L.GeoJSON<any>;

  constructor(private http: HttpClient) {
    this.loadVisitedCountries();
  }

  ngOnInit() {
    this.loadCountries();
  }

  ngAfterViewInit() {
    this.initializeMap();
    this.loadWorldKml();
  }

  private initializeMap() {
    this.map = L.map(this.mapContainer.nativeElement, {
      center: [20, 120],
      zoom: 2,
      minZoom: 2,
      maxZoom: 6,
      worldCopyJump: true,
    });

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; OpenStreetMap contributors',
      maxZoom: 18,
    }).addTo(this.map);
  }

  private loadWorldKml() {
    this.http.get('/countries.kml', { responseType: 'text' }).subscribe(
      (kmlText) => {
        try {
          const parser = new DOMParser();
          const xmlDoc = parser.parseFromString(kmlText, 'application/xml');
          const geojson = parseKml(xmlDoc) as any;

          this.kmlLayer = L.geoJSON(geojson, {
            filter: (feature) => {
              const type = feature.geometry?.type;
              if (!type) {
                return false;
              }
              if (type === 'Polygon' || type === 'MultiPolygon') {
                return true;
              }
              if (type === 'GeometryCollection' && Array.isArray(feature.geometry.geometries)) {
                return feature.geometry.geometries.some((item: any) => item.type === 'Polygon' || item.type === 'MultiPolygon');
              }
              return false;
            },
            style: (feature) => this.getFeatureStyle(feature),
            onEachFeature: (feature, layer) => {
              const pathLayer = layer as L.Path;
              const label = this.getFeatureLabel(feature);
              pathLayer.bindTooltip(`<span class="map-tooltip-label">${label}</span>`, {
                direction: 'center',
                className: 'map-tooltip',
                sticky: true,
                opacity: 0.95,
              });
              pathLayer.on('mouseover', () => {
                pathLayer.setStyle({ weight: 2, color: '#ffcc00' });
                pathLayer.openTooltip();
              });
              pathLayer.on('mouseout', () => {
                pathLayer.setStyle(this.getFeatureStyle(feature));
                pathLayer.closeTooltip();
              });
              pathLayer.on('click', () => {
                this.toggleFeatureVisited(feature);
                pathLayer.setStyle(this.getFeatureStyle(feature));
              });
            }
          }).addTo(this.map);

          this.refreshKmlStyles();
        } catch (error) {
          console.error('Failed to parse KML:', error);
          this.mapError = 'Unable to load the world map KML layer.';
        }
      },
      (error) => {
        console.error('Error loading KML:', error);
        this.mapError = 'Unable to load the world map KML layer.';
      }
    );
  }

  loadCountries() {
    this.http.get<any[]>('/countries').subscribe(
      (data) => {
        this.countries = data.map(country => ({
          name: country.name,
          code: country.code,
          capital: country.capital,
        })).sort((a, b) => a.name.localeCompare(b.name));
        this.countryNameToCode = new Map(this.countries.map(c => [this.normalizeName(c.name), c.code]));
        
        this.refreshKmlStyles();
      },
      (error) => {
        console.error('Error loading countries:', error);
      }
    );
  }

  toggleFeatureVisited(feature: any) {
    const rawName = this.getFeatureName(feature);
    const normalized = this.normalizeName(rawName);
    const code = this.countryNameToCode.get(normalized);
    if (!code) {
      return;
    }

    if (this.visitedCountries.has(code)) {
      this.visitedCountries.delete(code);
    } else {
      this.visitedCountries.add(code);
    }

    this.saveVisitedCountries();
    this.refreshKmlStyles();
  }

  private loadVisitedCountries() {
    const saved = localStorage.getItem('visitedCountries');
    if (saved) {
      this.visitedCountries = new Set(JSON.parse(saved));
    }
  }

  private saveVisitedCountries() {
    localStorage.setItem('visitedCountries', JSON.stringify(Array.from(this.visitedCountries)));
  }

  private normalizeName(name: string): string {
    return name
      .replace(/<[^>]+>/g, '')
      .replace(/\s+/g, ' ')
      .trim()
      .toLowerCase();
  }

  private getFeatureName(feature: any): string {
    return feature.properties?.name || feature.properties?.Name || 'Country';
  }

  private getFeatureLabel(feature: any): string {
    const rawName = this.getFeatureName(feature);
    const code = this.getCountryCode(feature);
    if (!code) {
      return rawName;
    }

    const country = this.countries.find(country => country.code === code);
    return country?.capital || rawName;
  }

  private getCountryCode(feature: any): string | null {
    const rawName = this.getFeatureName(feature);
    const normalized = this.normalizeName(rawName);
    const code = this.countryNameToCode.get(normalized);
    if (code) {
      return code;
    }

    const exact = this.countries.find(country => this.normalizeName(country.name) === normalized);
    if (exact) {
      return exact.code;
    }

    const partial = this.countries.find(country => {
      const continued = this.normalizeName(country.name);
      return continued.includes(normalized) || normalized.includes(continued);
    });
    return partial?.code ?? null;
  }

  private getFeatureStyle(feature: any): L.PathOptions {
    const rawName = this.getFeatureName(feature);
    const normalized = this.normalizeName(rawName);
    const code = this.countryNameToCode.get(normalized) || '';
    const visited = !!code && this.visitedCountries.has(code);

    if (visited) {
      return {
        color: '#0ea5e9',
        weight: 1,
        fillColor: '#34d399',
        fillOpacity: 0.45,
        interactive: true,
      } as unknown as L.PathOptions;
    }

    return {
      color: '#1f78b4',
      weight: 1,
      fillColor: '#a6cee3',
      fillOpacity: 0.15,
      interactive: true,
    } as unknown as L.PathOptions;
  }

  private refreshKmlStyles() {
    if (!this.kmlLayer) {
      return;
    }

    this.kmlLayer.eachLayer((layer: any) => {
      const feature = (layer as any).feature;
      if (layer instanceof L.Path && feature) {
        layer.setStyle(this.getFeatureStyle(feature));
      }
    });
  }

  getVisitedPercentage(): number {
    if (this.countries.length === 0) return 0;
    return Math.round((this.visitedCountries.size / this.countries.length) * 100);
  }
}
