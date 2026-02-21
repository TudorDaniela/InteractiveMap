import { ComponentFixture, TestBed } from '@angular/core/testing';

import { WorldMap } from './world-map';

describe('WorldMap', () => {
  let component: WorldMap;
  let fixture: ComponentFixture<WorldMap>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [WorldMap]
    })
    .compileComponents();

    fixture = TestBed.createComponent(WorldMap);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
