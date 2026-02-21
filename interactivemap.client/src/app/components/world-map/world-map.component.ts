import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';

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
  styleUrl: './world-map.component.css',
})
export class WorldMapComponent implements OnInit {
  countries: CountryWithVisited[] = [];
  hoveredCountry: CountryWithVisited | null = null;
  visitedCountries: Set<string> = new Set();
  loading = true;
  error: string | null = null;

  constructor(private http: HttpClient) {
    this.loadVisitedCountries();
  }

  ngOnInit() {
    this.loadCountries();
  }

  loadCountries() {
    this.http.get<any[]>('https://restcountries.com/v3.1/all').subscribe(
      (data) => {
        this.countries = data.map(country => ({
          name: country.name.common,
          code: country.cca2,
          capital: country.capital?.[0] || 'N/A',
          region: country.region || 'Unknown',
          visited: this.visitedCountries.has(country.cca2)
        })).sort((a, b) => a.name.localeCompare(b.name));
        this.loading = false;
      },
      (error) => {
        console.error('Error loading countries:', error);
        this.error = 'Failed to load countries data';
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
