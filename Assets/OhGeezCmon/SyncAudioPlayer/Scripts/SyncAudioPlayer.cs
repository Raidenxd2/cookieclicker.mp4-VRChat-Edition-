
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System;


/*
 * Sync Audio Player
 * 
 * Author: OhGeezCmon
 * 
 * Originally built back in the SDK2 days where I wanted multiple dance rooms in a world
 * but couldn't have more than one SyncVideo player going at a time.
 * Updated to Udon and it has much more accurate syncing, down to the second.
 * 
 * This will sync audio tracks BUILT IN to the world amongst all clients.  
 * i.e. MP3 or OGG files (all Unity supported audio formats)
 * Does NOT support audio hosted elsewhere (i.e. YouTube or Twitch)
 * For that use something like UdonSyncPlayer.
 * Might consider adding remote URLs in a future update
 * 
 * 
 * Update log:
 * 
 *      01.24.2021 (0.1) - Initial Check-in
 *                       - Supports Playlist input, NO CONTROLS yet.  Auto looping by default, not yet configurable
 */
public class SyncAudioPlayer : UdonSharpBehaviour
{
    public AudioSource audioSource;
    public AudioClip[] playList;
    [Tooltip("How often master syncs audio in seconds")]
    public int syncDuration = 5;
    [Tooltip("How far out of sync in seconds client can be before forced resync")]
    public int syncThreshold = 3;

    [UdonSynced] private string masterSyncInfo;
    private string prevMasterSyncInfo;
    private int masterSongIndex = 0;
    private float masterSongTimestamp = 0.0f;
    
    private float localSongTimestamp;
    private float localSongLength;
    private int localSongIndex = 0;
    
    private float nextActionTime = 0.0f; // Sync timer kicks off immediately
    private bool audioInitialized = false;

    /*
     * Changes tracks after a song finishes (auto loops playlist)
     * Also master updates sync data
     */ 
    private void Update()
    {
        if (audioSource == null || playList == null || playList.Length == 0)
            return;

        // All Players, if track is finished let's move onto the next one
        if (!audioSource.isPlaying && audioInitialized)
        {            
            if (localSongIndex == (playList.Length - 1)) // Loop if at end track
            {
                localSongIndex = 0;
            }
            else
            {
                localSongIndex++;
            }

            audioSource.time = 0.0f;  // Reset timer for next song
            localSongTimestamp = 0.0f;
            audioSource.clip = playList[localSongIndex];
            audioSource.Play();
            Debug.Log("[SyncAudioPlayer] Track finished, now playing track " + localSongIndex);
        }

        // Keep everyone up to date with sync timer 
        // Could become master at any point!
        if (Time.time > nextActionTime)
        {
            nextActionTime += (1.0f * syncDuration);

            if (Networking.IsMaster)
            {
                string netData = localSongIndex + "|" + audioSource.time;
                masterSyncInfo = netData;
                Debug.Log("[SyncAudioPlayer] Master Updating Sync Variable " + masterSyncInfo);
            }
        }
    }

    /*
     * Although this method fires for *every* player that joins a world
     * we are only interested in our own player object
     */
    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        if (audioSource == null || playList == null || playList.Length == 0 || Networking.LocalPlayer != player)
            return;

        // Master starts playing music, everyone else sync off master
        if (!audioInitialized)
        {
            if (Networking.IsMaster)
            {
                Debug.Log("[SyncAudioPlayer] player is master");
                audioSource.clip = playList[0];
                audioSource.time = 0.0f;
                audioSource.Play();
                audioInitialized = true;
            }
            else
            {
                // Don't play any music yet, wait for the first sync packet from master
                Debug.Log("[SyncAudioPlayer] Player is NOT master");
            }
        }
    }

    /*
     * Method fires everytime there is a sync variable change
     * 
     */
    public override void OnDeserialization()
    {
        // Master leads, doesn't follow
        if (Networking.IsMaster || masterSyncInfo == null)
            return;

        if (prevMasterSyncInfo != null && (masterSyncInfo == prevMasterSyncInfo)) // Haven't received new information yet
            return;

        // Deserialize the sync info string
        string[] masterSyncInfoArray = masterSyncInfo.Split('|');
        if (masterSyncInfoArray.Length != 2)
        {
            Debug.LogError("[SyncAudioPlayer] ERROR: masterSyncInfo deserialization error: " + masterSyncInfo);
            return;
        }

        if (!int.TryParse(masterSyncInfoArray[0], out masterSongIndex))
        {
            Debug.LogError("[SyncAudioPlayer] ERROR: masterSongIndex deserialization error: " + masterSyncInfoArray[0]);
            return;
        }

        if (!float.TryParse(masterSyncInfoArray[1], out masterSongTimestamp))
        {
            Debug.LogError("[SyncAudioPlayer] ERROR: masterSongTimestamp deserialization error: " + masterSyncInfoArray[1]);
            return;
        }

        prevMasterSyncInfo = masterSyncInfo; // syncInfo is good, update checkpoint

        int syncCase = -1;
        localSongTimestamp = audioSource.time;

        // First check the song index to make sure we're playing the right track
        if (masterSongIndex != localSongIndex)
        {
            // There are two cases here:
            // 1. Client is way off because they just arrived or had a massive de-sync, in which case let's sync up
            // 2. Client is slightly behind, finishing the last few seconds of track N when Master is at N+1 or 0 if master has looped the playlist
            //      Don't sync in this case otherwise it's jarring to client
            localSongLength = playList[localSongIndex].length;

            if (localSongLength - localSongTimestamp > syncThreshold) // Case 1
            {
                syncCase = 1;
            }
            else // Case 2
            {
                syncCase = 2;
            }
        }
        // Same track but out of sync
        else if (Mathf.Abs(masterSongTimestamp - localSongTimestamp) > syncThreshold)
        {
            syncCase = 1;
        }

        // To sync or not to sync; that is the question.
        switch (syncCase)
        {
            case 1:
                Debug.LogFormat("[SyncAudioPlayer] Syncing audio to master at track {0}, masterSongTimeStamp: {1}, localSongTimeStamp: {2}, Delta: {3}, Threshold: {4}", localSongIndex, masterSongTimestamp, localSongTimestamp, Mathf.Abs(masterSongTimestamp - localSongTimestamp), syncThreshold);
                localSongIndex = masterSongIndex;
                localSongTimestamp = masterSongTimestamp;
                audioSource.clip = playList[masterSongIndex];
                audioSource.time = masterSongTimestamp;
                audioSource.Play();
                audioInitialized = true;                
                break;
            case 2:
                Debug.LogFormat("[SyncAudioPlayer] Client finishing track, not interrupting. Track length: {0} Track Timestamp: {1} Delta: {2} Threshold: {3}", localSongLength, localSongTimestamp, (localSongLength - localSongTimestamp), syncThreshold);
                break;
            default:
                // Do nothing we are in sync, baby!
                break;
        }
    }
}
