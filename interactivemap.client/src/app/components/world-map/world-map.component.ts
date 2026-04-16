import { AfterViewInit, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as L from 'leaflet';
import { kml as parseKml } from '@mapbox/togeojson';

interface Country {
  name: string;
  code: string;
  capital: string;
  region: string;
}

interface CountryWithVisited extends Country {
  visited: boolean;
}

@Component({
  selector: 'app-world-map',
  standalone: false,
  templateUrl: './world-map.component.html',
  styleUrls: ['./world-map.component.css'],
})
export class WorldMapComponent implements OnInit, AfterViewInit {
  @ViewChild('mapContainer', { static: false }) mapContainer!: ElementRef<HTMLDivElement>;

  countries: CountryWithVisited[] = [];
  hoveredCountry: CountryWithVisited | null = null;
  visitedCountries: Set<string> = new Set();
  loading = true;
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
      center: [20, 0],
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
            style: {
              color: '#1f78b4',
              weight: 1,
              fillColor: '#a6cee3',
              fillOpacity: 0.15,
            },
            onEachFeature: (feature, layer) => {
              const name = feature.properties?.name || feature.properties?.Name || 'Country';
              layer.bindPopup(`<strong>${name}</strong>`);
              if (layer instanceof L.Path) {
                layer.on('mouseover', () => {
                  layer.setStyle({ weight: 2, color: '#ff6600' });
                });
                layer.on('mouseout', () => {
                  layer.setStyle({ weight: 1, color: '#1f78b4' });
                });
              }
            }
          }).addTo(this.map);

          const bounds = this.kmlLayer.getBounds();
          if (bounds.isValid()) {
            this.map.fitBounds(bounds, { padding: [20, 20] });
          }
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
          region: country.region,
          visited: this.visitedCountries.has(country.code)
        })).sort((a, b) => a.name.localeCompare(b.name));
        this.loading = false;
      },
      (error) => {
        console.error('Error loading countries:', error);
        this.loading = false;
      }
    );
  }

  onCountryHover(country: CountryWithVisited) {
    this.hoveredCountry = country;
  }

  onCountryLeave() {
    this.hoveredCountry = null;
  }

  toggleCountryVisited(country: CountryWithVisited, event: Event) {
    event.stopPropagation();
    country.visited = !country.visited;

    if (country.visited) {
      this.visitedCountries.add(country.code);
    } else {
      this.visitedCountries.delete(country.code);
    }

    this.saveVisitedCountries();
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

  getVisitedPercentage(): number {
    if (this.countries.length === 0) return 0;
    return Math.round((this.visitedCountries.size / this.countries.length) * 100);
  }
}
