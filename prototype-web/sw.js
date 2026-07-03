/* 다이스 던전 v0.8.0 — 서비스 워커: index.html 캐시-퍼스트 오프라인 플레이 */
'use strict';

const CACHE = 'dice-dungeon-v0.8.0';
const ASSETS = ['./', './index.html', './manifest.json'];

self.addEventListener('install', event => {
  event.waitUntil(
    caches.open(CACHE)
      .then(c => c.addAll(ASSETS))
      .catch(() => { /* 일부 실패해도 설치는 계속 */ })
  );
  self.skipWaiting();
});

self.addEventListener('activate', event => {
  event.waitUntil(
    caches.keys()
      .then(keys => Promise.all(keys.filter(k => k !== CACHE).map(k => caches.delete(k))))
      .then(() => self.clients.claim())
  );
});

/* 캐시-퍼스트: 캐시 히트 즉시 반환 → 미스면 네트워크(성공 시 캐시에 적재) → 오프라인 내비게이션은 index.html 폴백 */
self.addEventListener('fetch', event => {
  if (event.request.method !== 'GET') return;
  event.respondWith(
    caches.match(event.request).then(hit => {
      if (hit) return hit;
      return fetch(event.request).then(res => {
        if (res && res.ok && new URL(event.request.url).origin === self.location.origin) {
          const copy = res.clone();
          caches.open(CACHE).then(c => c.put(event.request, copy)).catch(() => {});
        }
        return res;
      }).catch(() => {
        if (event.request.mode === 'navigate') return caches.match('./index.html');
        return Response.error();
      });
    })
  );
});
