import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class GitLabHelperService {
  public static gitLabUrlBase: string = GitLabHelperService.resolveGitLabUrlBase();

  private static resolveGitLabUrlBase(): string {
    const gitLabPort = window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1'
      ? '8082'
      : '18082';
    return `${window.location.protocol}//${window.location.hostname}:${gitLabPort}`;
  }

  public static makeGroupLink(groupName: string): string {
    return `${GitLabHelperService.gitLabUrlBase}/${groupName}`;
  }

  public static makeUserLink(username: string): string {
    return `${GitLabHelperService.gitLabUrlBase}/${username}`;
  }

  public static fromUrl(rawUrl: string) {
    const url = new URL(rawUrl);
    return `${GitLabHelperService.gitLabUrlBase}${url.pathname}`;
  }

  constructor() { }
}
