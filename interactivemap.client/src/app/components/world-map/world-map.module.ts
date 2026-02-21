import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { WorldMapComponent } from './world-map.component';

@NgModule({
  imports: [CommonModule],
  declarations: [WorldMapComponent],
  exports: [WorldMapComponent]
})
export class WorldMapModule {}
