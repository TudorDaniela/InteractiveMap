import { HttpClientModule } from '@angular/common/http';
import { NgModule, provideBrowserGlobalErrorListeners } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing-module';
import { App } from './app';
import { WorldMapModule } from './components/world-map/world-map.module';

@NgModule({
  declarations: [
    App,
  ],
  imports: [
    BrowserModule, HttpClientModule,
    AppRoutingModule,
    WorldMapModule,
  ],
  providers: [
    provideBrowserGlobalErrorListeners()
  ],
  bootstrap: [App]
})
export class AppModule { }
