.class public Lcom/skandax/afterzero/Bridge;
.super Ljava/lang/Object;
.source "Bridge.java"

.field private act:Landroid/app/Activity;

.method public constructor <init>(Landroid/app/Activity;)V
    .registers 2
    invoke-direct {p0}, Ljava/lang/Object;-><init>()V
    iput-object p1, p0, Lcom/skandax/afterzero/Bridge;->act:Landroid/app/Activity;
    return-void
.end method

.method public vibrate(I)V
    .registers 6
    .annotation runtime Landroid/webkit/JavascriptInterface;
    .end annotation
    :try_start
    iget-object v0, p0, Lcom/skandax/afterzero/Bridge;->act:Landroid/app/Activity;
    const-string v1, "vibrator"
    invoke-virtual {v0, v1}, Landroid/app/Activity;->getSystemService(Ljava/lang/String;)Ljava/lang/Object;
    move-result-object v0
    check-cast v0, Landroid/os/Vibrator;
    if-eqz v0, :end
    int-to-long v2, p1
    invoke-virtual {v0, v2, v3}, Landroid/os/Vibrator;->vibrate(J)V
    :try_end
    .catch Ljava/lang/Throwable; {:try_start .. :try_end} :end
    :end
    return-void
.end method

.method public quit()V
    .registers 2
    .annotation runtime Landroid/webkit/JavascriptInterface;
    .end annotation
    iget-object v0, p0, Lcom/skandax/afterzero/Bridge;->act:Landroid/app/Activity;
    invoke-virtual {v0}, Landroid/app/Activity;->finish()V
    return-void
.end method

# window.AZNative.setOrientation(code): ActivityInfo.SCREEN_ORIENTATION_* (10 fullSensor, 6 sensorLandscape, 7 sensorPortrait)
.method public setOrientation(I)V
    .registers 3
    .annotation runtime Landroid/webkit/JavascriptInterface;
    .end annotation
    :try_start
    iget-object v0, p0, Lcom/skandax/afterzero/Bridge;->act:Landroid/app/Activity;
    invoke-virtual {v0, p1}, Landroid/app/Activity;->setRequestedOrientation(I)V
    :try_end
    .catch Ljava/lang/Throwable; {:try_start .. :try_end} :end
    :end
    return-void
.end method
